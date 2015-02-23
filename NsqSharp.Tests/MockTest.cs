using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NsqSharp.Channels;
using NsqSharp.Extensions;
using NsqSharp.Go;
using NsqSharp.Tests.Utils;
using NsqSharp.Utils;
using NUnit.Framework;

namespace NsqSharp.Tests
{
    [TestFixture]
    public class MockTest
    {
        [Test]
        public void TestConsumerBackoff()
        {
            var mgood = new MemoryStream();
            var msgIDGood = Encoding.UTF8.GetBytes("1234567890asdfgh");
            var msgGood = new Message(msgIDGood, Encoding.UTF8.GetBytes("good"));
            msgGood.WriteTo(mgood);
            var msgBytesGood = mgood.ToArray();
            mgood.Dispose();

            var mbad = new MemoryStream();
            var msgIDBad = Encoding.UTF8.GetBytes("zxcvb67890asdfgh");
            var msgBad = new Message(msgIDBad, Encoding.UTF8.GetBytes("bad"));
            msgBad.WriteTo(mbad);
            var msgBytesBad = mbad.ToArray();
            mbad.Dispose();

            var script = new[]
                         {
                            // SUB
                            new instruction(0, FrameType.Response, "OK"),
                            // IDENTIFY
                            new instruction(0, FrameType.Response, "OK"),
                            new instruction(100 * Time.Millisecond, FrameType.Message, msgBytesGood),
                            new instruction(100 * Time.Millisecond, FrameType.Message, msgBytesGood),
                            new instruction(100 * Time.Millisecond, FrameType.Message, msgBytesGood),
                            new instruction(100 * Time.Millisecond, FrameType.Message, msgBytesBad),
                            new instruction(100 * Time.Millisecond, FrameType.Message, msgBytesBad),
                            new instruction(100 * Time.Millisecond, FrameType.Message, msgBytesGood),
                            new instruction(100 * Time.Millisecond, FrameType.Message, msgBytesGood),
                            // needed to exit test
                            new instruction(1000 * Time.Millisecond, -1, "exit")
                         };

            var n = new mockNSQD(script);

            var topicName = "test_consumer_commands" + DateTime.Now.Unix();
            var config = new Config();
            config.MaxInFlight = 5;
            config.BackoffMultiplier = Time.Duration(10 * Time.Millisecond);
            var q = new Consumer(topicName, "ch", config);
            q.SetLogger(new ConsoleLogger(), LogLevel.Debug);
            q.AddHandler(new testHandler());
            q.ConnectToNSQD(n.tcpAddr);

            n.exitChan.Receive();

            for (int i = 0; i < n.got.Count; i++)
            {
                log.Printf("{0}: {1}", i, Encoding.UTF8.GetString(n.got[i]));
            }

            var expected = new[]
                           {
                                "IDENTIFY",
                                "SUB " + topicName + " ch",
                                "RDY 5",
                                string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                                string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                                string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                                "RDY 5",
                                "RDY 0",
                                string.Format("REQ {0} 0", Encoding.UTF8.GetString(msgIDBad)),
                                "RDY 1",
                                "RDY 0",
                                string.Format("REQ {0} 0", Encoding.UTF8.GetString(msgIDBad)),
                                "RDY 1",
                                "RDY 0",
                                string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                                "RDY 1",
                                "RDY 5",
                                string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                           };

            var actual = new List<string>();
            foreach (var g in n.got)
            {
                actual.Add(Encoding.UTF8.GetString(g));
            }

            Assert.AreEqual(expected, actual);
        }

        public class testHandler : IHandler
        {
            public void HandleMessage(Message message)
            {
                string body = Encoding.UTF8.GetString(message.Body);
                log.Printf(body);
                if (body != "good")
                {
                    throw new Exception("bad");
                }
            }

            public void LogFailedMessage(Message message)
            {
            }
        }

        public class instruction
        {
            public TimeSpan delay { get; set; }
            public int frameType { get; set; }
            public byte[] body { get; set; }

            public instruction(long delay, int frameType, byte[] body)
            {
                this.delay = Time.Duration(delay);
                this.frameType = frameType;
                this.body = body;
            }

            public instruction(long delay, int frameType, string body)
                : this(delay, frameType, Encoding.UTF8.GetBytes(body))
            {
            }

            public instruction(long delay, FrameType frameType, byte[] body)
                : this(delay, (int)frameType, body)
            {
            }

            public instruction(long delay, FrameType frameType, string body)
                : this(delay, (int)frameType, Encoding.UTF8.GetBytes(body))
            {
            }
        }

        public class mockNSQD
        {
            private instruction[] script { get; set; }
            private TcpListener tcpListener { get; set; }

            public List<byte[]> got { get; set; }
            public string tcpAddr { get; set; }
            public Chan<int> exitChan { get; set; }

            public mockNSQD(instruction[] script)
            {
                this.script = script;
                exitChan = new Chan<int>();
                got = new List<byte[]>();
                GoFunc.Run(listen);
            }

            private void listen()
            {
                tcpListener = new TcpListener(IPAddress.Loopback, 4152);
                tcpListener.Start();
                tcpAddr = tcpListener.LocalEndpoint.ToString();

                log.Printf("TCP: listening on {0}", tcpListener.LocalEndpoint);

                while (true)
                {
                    TcpClient conn;
                    try
                    {
                        conn = tcpListener.AcceptTcpClient();
                    }
                    catch
                    {
                        break;
                    }
                    GoFunc.Run(() => handle(conn));
                }

                log.Printf("TCP: closing {0}", tcpListener.LocalEndpoint);
                exitChan.Close();
            }

            private void handle(TcpClient conn)
            {
                int idx = 0;

                log.Printf("TCP: new client({0})", conn.Client.RemoteEndPoint);

                using (var rdr = new BinaryReader(conn.GetStream()))
                using (var connw = new BinaryWriter(conn.GetStream()))
                {
                    rdr.ReadBytes(4);

                    var readChan = new Chan<byte[]>();
                    var readDoneChan = new Chan<int>();
                    var scriptTime = Time.After(script[0].delay);

                    GoFunc.Run(() =>
                               {
                                   while (true)
                                   {
                                       try
                                       {
                                           var line = ReadBytes(rdr, (byte)'\n');
                                           // trim the '\n'
                                           line = line.Take(line.Length - 1).ToArray();
                                           readChan.Send(line);
                                           readDoneChan.Receive();
                                       }
                                       catch
                                       {
                                           return;
                                       }
                                   }
                               });

                    int rdyCount = 0;
                    bool doLoop = true;
                    while (doLoop && idx < script.Length)
                    {
                        Select
                            .CaseReceive(readChan, line =>
                            {
                                string strLine = Encoding.UTF8.GetString(line);
                                log.Printf("mock: '{0}'", strLine);
                                got.Add(line);
                                var args = strLine.Split(new[] { ' ' });
                                switch (args[0])
                                {
                                    case "IDENTIFY":
                                        try
                                        {
                                            byte[] l = rdr.ReadBytes(4);
                                            int size = Binary.BigEndian.Int32(l);
                                            byte[] b = rdr.ReadBytes(size);
                                            log.Printf(Encoding.UTF8.GetString(b));
                                        }
                                        catch (Exception ex)
                                        {
                                            log.Printf(ex.ToString());
                                            doLoop = false;
                                            throw;
                                        }
                                        break;
                                    case "RDY":
                                        int rdy = int.Parse(args[1]);
                                        rdyCount = rdy;
                                        break;
                                }
                                readDoneChan.Send(1);
                            })
                            .CaseReceive(scriptTime, o =>
                            {
                                bool doWrite = true;
                                var inst = script[idx];
                                if (inst.body.SequenceEqual(Encoding.UTF8.GetBytes("exit")))
                                {
                                    doLoop = false;
                                    doWrite = false;
                                }
                                if (inst.frameType == (int)FrameType.Message)
                                {
                                    if (rdyCount == 0)
                                    {
                                        //log.Printf("!!! RDY == 0");
                                        scriptTime = Time.After(script[idx + 1].delay);
                                        doWrite = false;
                                    }
                                    else
                                    {
                                        rdyCount--;
                                    }
                                }

                                if (doWrite)
                                {
                                    try
                                    {
                                        connw.Write(framedResponse(inst.frameType, inst.body));
                                        scriptTime = Time.After(script[idx + 1].delay);
                                        idx++;
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Printf(ex.ToString());
                                        doLoop = false;
                                    }
                                }
                            })
                            .NoDefault();
                    }
                }

                tcpListener.Stop();
            }

            private byte[] framedResponse(int frameType, byte[] data)
            {
                using (var ms = new MemoryStream())
                using (var w = new BinaryWriter(ms))
                {
                    byte[] beBuf = new byte[4];
                    int size = data.Length + 4;

                    Binary.BigEndian.PutUint32(beBuf, size);
                    w.Write(beBuf);

                    Binary.BigEndian.PutUint32(beBuf, frameType);
                    w.Write(beBuf);

                    w.Write(data);

                    return ms.ToArray();
                }
            }

            private byte[] ReadBytes(BinaryReader rdr, byte stop)
            {
                using (var memoryStream = new MemoryStream())
                {
                    while (true)
                    {
                        var b = rdr.ReadByte();
                        memoryStream.WriteByte(b);
                        if (b == stop)
                            return memoryStream.ToArray();
                    }
                }
            }
        }
    }
}
