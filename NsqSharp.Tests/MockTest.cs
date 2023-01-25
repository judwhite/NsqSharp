﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NsqSharp.Core;
using NsqSharp.Tests.Utils.Extensions;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NsqSharp.Utils.Extensions;
using NsqSharp.Utils.Loggers;
using NUnit.Framework;

namespace NsqSharp.Tests
{
#if !RUN_INTEGRATION_TESTS
    [Ignore("NSQD Integration Test")]
#endif
    [TestFixture]
    public class MockTest
    {
        private static byte[] frameMessage(Message m)
        {
            using (var memoryStream = new MemoryStream())
            {
                m.WriteTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        [Test]
        public void TestConsumerBackoff()
        {
            var msgIDGood = Encoding.UTF8.GetBytes("1234567890asdfgh");
            var msgGood = new Message(msgIDGood, Encoding.UTF8.GetBytes("good"));

            var msgIDBad = Encoding.UTF8.GetBytes("zxcvb67890asdfgh");
            var msgBad = new Message(msgIDBad, Encoding.UTF8.GetBytes("bad"));

            var script = new[]
                         {
                             // SUB
                             new instruction(0, FrameType.Response, "OK"),
                             // IDENTIFY
                             new instruction(0, FrameType.Response, "OK"),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgBad)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgBad)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             // needed to exit test
                             new instruction(200 * Time.Millisecond, -1, "exit")
                         };
            var n = new mockNSQD(script, IPAddress.Loopback);

            var topicName = "test_consumer_commands" + DateTime.Now.Unix();
            var config = new Config();
            config.MaxInFlight = 5;
            config.BackoffMultiplier = Time.Duration(10 * Time.Millisecond);
            var q = new Consumer(topicName, "ch", new ConsoleLogger(Core.LogLevel.Debug), config);
            q.AddHandler(new testHandler());
            q.ConnectToNsqd(n.tcpAddr);

            bool timeout = false;
            Select
                .CaseReceive(n.exitChan, o => { })
                .CaseReceive(Time.After(TimeSpan.FromMilliseconds(5000)), o => { timeout = true; })
                .NoDefault();

            Assert.IsFalse(timeout, "timeout");

            for (int i = 0; i < n.got.Count; i++)
            {
                Console.WriteLine("[{0}] {1}: {2}", n.gotTime[i].Formatted(), i, Encoding.UTF8.GetString(n.got[i]));
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

            q.DisconnectFromNsqd(n.tcpAddr);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestConsumerRequeueNoBackoff()
        {
            var msgIDGood = Encoding.UTF8.GetBytes("1234567890asdfgh");
            var msgIDRequeue = Encoding.UTF8.GetBytes("reqvb67890asdfgh");
            var msgIDRequeueNoBackoff = Encoding.UTF8.GetBytes("reqnb67890asdfgh");

            var msgGood = new Message(msgIDGood, Encoding.UTF8.GetBytes("good"));
            var msgRequeue = new Message(msgIDRequeue, Encoding.UTF8.GetBytes("requeue"));
            var msgRequeueNoBackoff = new Message(msgIDRequeueNoBackoff, Encoding.UTF8.GetBytes("requeue_no_backoff_1"));

            var script = new[]
                         {
                             // SUB
                             new instruction(0, FrameType.Response, "OK"),
                             // IDENTIFY
                             new instruction(0, FrameType.Response, "OK"),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgRequeue)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgRequeueNoBackoff)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             // needed to exit test
                             new instruction(100 * Time.Millisecond, -1, "exit")
                         };

            var n = new mockNSQD(script, IPAddress.Loopback);

            var topicName = "test_requeue" + DateTime.Now.Unix();
            var config = new Config();
            config.MaxInFlight = 1;
            config.BackoffMultiplier = Time.Duration(10 * Time.Millisecond);
            var q = new Consumer(topicName, "ch", new ConsoleLogger(Core.LogLevel.Debug), config);
            q.AddHandler(new testHandler());
            q.ConnectToNsqd(n.tcpAddr);

            bool timeout = false;
            Select
                .CaseReceive(n.exitChan, o => { })
                .CaseReceive(Time.After(TimeSpan.FromMilliseconds(500)), o => { timeout = true; })
                .NoDefault();

            Assert.IsFalse(timeout, "timeout");

            for (int i = 0; i < n.got.Count; i++)
            {
                Console.WriteLine("[{0}] {1}: {2}", n.gotTime[i].Formatted(), i, Encoding.UTF8.GetString(n.got[i]));
            }

            var expected = new[]
                           {
                               "IDENTIFY",
                               "SUB " + topicName + " ch",
                               "RDY 1",
                               "RDY 1",
                               "RDY 0",
                               string.Format("REQ {0} 0", Encoding.UTF8.GetString(msgIDRequeue)),
                               "RDY 1",
                               "RDY 0",
                               string.Format("REQ {0} 0", Encoding.UTF8.GetString(msgIDRequeueNoBackoff)),
                               "RDY 1",
                               "RDY 1",
                               string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                           };

            var actual = new List<string>();
            foreach (var g in n.got)
            {
                actual.Add(Encoding.UTF8.GetString(g));
            }

            Assert.AreEqual(expected, actual);
            q.Stop();
        }

        [Test]
        public void TestConsumerBackoffDisconnect()
        {
            var msgIDGood = Encoding.UTF8.GetBytes("1234567890asdfgh");
            var msgIDRequeue = Encoding.UTF8.GetBytes("reqvb67890asdfgh");

            var msgGood = new Message(msgIDGood, Encoding.UTF8.GetBytes("good"));
            var msgRequeue = new Message(msgIDRequeue, Encoding.UTF8.GetBytes("requeue"));

            var script = new[]
                         {
                             // SUB
                             new instruction(0, FrameType.Response, "OK"),
                             // IDENTIFY
                             new instruction(0, FrameType.Response, "OK"),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgRequeue)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgRequeue)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             // needed to exit test
                             new instruction(100 * Time.Millisecond, -1, "exit")
                         };

            var n = new mockNSQD(script, IPAddress.Loopback);

            var topicName = "test_backoff_disconnect" + DateTime.Now.Unix();
            var config = new Config();
            config.MaxInFlight = 5;
            config.BackoffMultiplier = Time.Duration(10 * Time.Millisecond);
            config.LookupdPollInterval = Time.Duration(10 * Time.Millisecond);
            config.RDYRedistributeInterval = Time.Duration(10 * Time.Millisecond);
            var q = new Consumer(topicName, "ch", new ConsoleLogger(Core.LogLevel.Debug), config);
            q.AddHandler(new testHandler());
            q.ConnectToNsqd(n.tcpAddr);

            bool timeout = false;
            Select
                .CaseReceive(n.exitChan, o => { })
                .CaseReceive(Time.After(TimeSpan.FromMilliseconds(500)), o => { timeout = true; })
                .NoDefault();

            Assert.IsFalse(timeout, "timeout");

            for (int i = 0; i < n.got.Count; i++)
            {
                Console.WriteLine("[{0}] {1}: {2}", n.gotTime[i].Formatted(), i, Encoding.UTF8.GetString(n.got[i]));
            }

            var expected = new[]
                           {
                               "IDENTIFY",
                               "SUB " + topicName + " ch",
                               "RDY 5",
                               string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                               "RDY 0",
                               string.Format("REQ {0} 0", Encoding.UTF8.GetString(msgIDRequeue)),
                               "RDY 1",
                               "RDY 0",
                               string.Format("REQ {0} 0", Encoding.UTF8.GetString(msgIDRequeue)),
                               "RDY 1",
                               "RDY 0",
                               string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                               "RDY 1",
                           };

            var actual = new List<string>();
            foreach (var g in n.got)
            {
                actual.Add(Encoding.UTF8.GetString(g));
            }

            Assert.AreEqual(expected, actual, "test1 failed");

            ///////

            script = new[]
                         {
                             // SUB
                             new instruction(0, FrameType.Response, "OK"),
                             // IDENTIFY
                             new instruction(0, FrameType.Response, "OK"),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             new instruction(20 * Time.Millisecond, FrameType.Message, frameMessage(msgGood)),
                             // needed to exit test
                             new instruction(100 * Time.Millisecond, -1, "exit")
                         };

            n = new mockNSQD(script, IPAddress.Loopback, n.listenPort);

            bool timeout2 = false;
            Select
                .CaseReceive(n.exitChan, o => { })
                .CaseReceive(Time.After(TimeSpan.FromMilliseconds(500)), o => { timeout2 = true; })
                .NoDefault();

            Assert.IsFalse(timeout2, "timeout2");

            for (int i = 0; i < n.got.Count; i++)
            {
                Console.WriteLine("[{0}] {1}: {2}", DateTime.Now.Formatted(), i, Encoding.UTF8.GetString(n.got[i]));
            }

            expected = new[]
                           {
                               "IDENTIFY",
                               "SUB " + topicName + " ch",
                               "RDY 1",
                               "RDY 5",
                               string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                               string.Format("FIN {0}", Encoding.UTF8.GetString(msgIDGood)),
                           };

            actual = new List<string>();
            foreach (var g in n.got)
            {
                actual.Add(Encoding.UTF8.GetString(g));
            }

            Assert.AreEqual(expected, actual, "test2 failed");
            q.Stop();
        }
    }

    public class testHandler : IHandler
    {
        public void HandleMessage(IMessage message)
        {
            string body = Encoding.UTF8.GetString(message.Body);
            Console.WriteLine("[{0}] {1}", DateTime.Now.Formatted(), body);

            switch (body)
            {
                case "requeue":
                    message.Requeue();
                    break;
                case "requeue_no_backoff_1":
                    if (message.Attempts > 1)
                        break;
                    message.RequeueWithoutBackoff(delay: null);
                    break;
                case "good":
                    break;
                case "bad":
                    throw new Exception("bad");
                default:
                    throw new InvalidOperationException(string.Format("body '{0}' not recognized", body));
            }
        }

        public void LogFailedMessage(IMessage message)
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
        private IPAddress ipAddr { get; set; }

        public List<byte[]> got { get; set; }
        public List<DateTime> gotTime { get; set; }
        public string tcpAddr { get; set; }
        public int listenPort { get; private set; }
        public Chan<int> exitChan { get; set; }

        public mockNSQD(instruction[] script, IPAddress addr, int port = 0)
        {
            this.script = script;
            exitChan = new Chan<int>();
            got = new List<byte[]>();
            gotTime = new List<DateTime>();
            ipAddr = addr;
            tcpListener = new TcpListener(ipAddr, port);
            tcpListener.Start();
            tcpAddr = tcpListener.LocalEndpoint.ToString();
            listenPort = ((IPEndPoint)tcpListener.LocalEndpoint).Port;

            GoFunc.Run(listen, "mockNSQD:listen");
        }

        private void listen()
        {
            var addr = tcpListener.LocalEndpoint;
            Console.WriteLine("[{0}] TCP: listening on {1}", DateTime.Now.Formatted(), addr);

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
                var remoteEndPoint = conn.Client.RemoteEndPoint;
                GoFunc.Run(() => handle(conn, remoteEndPoint), "mockNSQD:handle");
            }

            Console.WriteLine("[{0}] TCP: closing {1}", DateTime.Now.Formatted(), addr);
            exitChan.Close();
        }

        private void handle(TcpClient conn, EndPoint remoteEndPoint)
        {
            int idx = 0;

            Console.WriteLine("[{0}] TCP: new client({1})", DateTime.Now.Formatted(), remoteEndPoint);

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
                           }, "mockNSQD:ReadBytes");

                int rdyCount = 0;
                bool doLoop = true;

                while (doLoop && idx < script.Length)
                {
                    Select
                        .CaseReceive(readChan, line =>
                        {
                            string strLine = Encoding.UTF8.GetString(line);
                            Console.WriteLine("[{0}] mock: '{1}'", DateTime.Now.Formatted(), strLine);
                            got.Add(line);
                            gotTime.Add(DateTime.Now);
                            var args = strLine.Split(' ');
                            switch (args[0])
                            {
                                case "IDENTIFY":
                                    try
                                    {
                                        byte[] l = rdr.ReadBytes(4);
                                        int size = Binary.BigEndian.Int32(l);
                                        byte[] b = rdr.ReadBytes(size);

                                        Console.WriteLine(string.Format("[{0}] {1}",
                                            DateTime.Now.Formatted(), Encoding.UTF8.GetString(b)));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
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
                                    Console.WriteLine("[{0}] !!! RDY == 0", DateTime.Now.Formatted());
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
                                    Console.WriteLine(ex.ToString());
                                    doLoop = false;
                                }
                            }
                        })
                        .NoDefault();
                }
            }

            tcpListener.Stop();
            conn.Close();
        }

        internal static byte[] framedResponse(int frameType, byte[] data)
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
