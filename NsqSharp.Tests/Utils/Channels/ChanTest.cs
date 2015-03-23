using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NUnit.Framework;

namespace NsqSharp.Tests.Utils.Channels
{
    [TestFixture]
    public class ChanTest
    {
        [Test]
        public void SingleNumberGenerator()
        {
            var c = new Chan<int>();

            var t = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    c.Send(i);
                }
                c.Close();
            });
            t.IsBackground = true;
            t.Start();

            var list = new List<int>();
            foreach (var i in c)
            {
                list.Add(i);
            }

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list);
        }

        [Test]
        public void MultipleNumberGenerators()
        {
            var c = new Chan<int>();

            for (int i = 0; i < 10; i++)
            {
                int localNum = i;

                var t = new Thread(() => c.Send(localNum));
                t.IsBackground = true;
                t.Start();
            }

            var list = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(c.Receive());
            }

            list.Sort();

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list);
        }

        [Test]
        public void PrimeSieve()
        {
            var generate = new Action<Chan<int>>(cgen =>
                           {
                               while (true)
                               {
                                   for (int i = 2; ; i++)
                                   {
                                       cgen.Send(i);
                                   }
                               }
                               // ReSharper disable once FunctionNeverReturns
                           });

            var filter = new Action<Chan<int>, Chan<int>, int>((cin, cout, prime) =>
                                                               {
                                                                   while (true)
                                                                   {
                                                                       var i = cin.Receive();
                                                                       if (i % prime != 0)
                                                                       {
                                                                           cout.Send(i);
                                                                       }
                                                                   }
                                                                   // ReSharper disable once FunctionNeverReturns
                                                               });

            var ch = new Chan<int>();

            Chan<int> generateCh = ch;
            var threadGenerate = new Thread(() => generate(generateCh));
            threadGenerate.IsBackground = true;
            threadGenerate.Start();

            var list = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                int prime = ch.Receive();
                list.Add(prime);

                var ch0 = ch;
                var ch1 = new Chan<int>();

                var threadFilter = new Thread(() => filter(ch0, ch1, prime));
                threadFilter.IsBackground = true;
                threadFilter.Start();

                ch = ch1;
            }

            Assert.AreEqual(new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 }, list);
        }

        [Test]
        public void SelectTwoChannels()
        {
            var c1 = new Chan<int>();
            var c2 = new Chan<int>();

            var t1 = new Thread(() =>
                                {
                                    Thread.Sleep(10);
                                    c1.Send(1);
                                });
            t1.IsBackground = true;

            var t2 = new Thread(() => c2.Send(2));
            t2.IsBackground = true;

            var list = new List<int>();

            t1.Start();
            t2.Start();

            Select
                .DebugName("SelectTwoChannels")
                .CaseReceive("c1", c1, list.Add)
                .CaseReceive("c2", c2, list.Add)
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(2, list[0], "list[0]");
        }

        [Test]
        public void SelectNullChannel()
        {
            var c1 = new Chan<int>();

            var t1 = new Thread(() =>
            {
                Thread.Sleep(10);
                c1.Send(3);
            });
            t1.IsBackground = true;

            var list = new List<int>();

            t1.Start();

            Select
                .DebugName("SelectNullChannel")
                .CaseReceive("c2", (Chan<int>)null, list.Add)
                .CaseReceive("c1", c1, list.Add)
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(3, list[0], "list[0]");
        }

        [Test]
        public void SelectDefaultCaseNoChannelsReady()
        {
            var c1 = new Chan<int>();
            var c2 = new Chan<int>();

            var t1 = new Thread(() =>
            {
                Thread.Sleep(10);
                c1.Send(1);
            });
            t1.IsBackground = true;

            var t2 = new Thread(() =>
            {
                Thread.Sleep(10);
                c2.Send(2);
            });
            t2.IsBackground = true;

            var list = new List<int>();

            t1.Start();
            t2.Start();

            Select
                .DebugName("SelectDefaultCaseNoChannelsReady")
                .CaseReceive("c1", c1, list.Add)
                .CaseReceive("c2", c2, list.Add)
                .Default(() => list.Add(3));

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(3, list[0], "list[0]");
        }

        [Test]
        public void SelectDefaultCaseChannelReady()
        {
            var c1 = new Chan<int>();
            var c2 = new Chan<int>();

            var t1 = new Thread(() =>
            {
                Thread.Sleep(100);
                c1.Send(1);
            });
            t1.IsBackground = true;

            var t2 = new Thread(() => c2.Send(5));
            t2.IsBackground = true;

            var list = new List<int>();

            t1.Start();
            t2.Start();

            Thread.Sleep(50);

            Select
                .DebugName("SelectDefaultCaseChannelReady")
                .CaseReceive("c1", c1, list.Add)
                .CaseReceive("c2", c2, list.Add)
                .Default(() => list.Add(3));

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(5, list[0], "list[0]");
        }

        [Test]
        public void SendOnClosedChannelThrows()
        {
            var c = new Chan<int>();
            c.Close();

            Assert.Throws<ChannelClosedException>(() => c.Send(1));
        }

        [Test]
        public void ReceiveOnClosedChannelReturnsDefault()
        {
            var c = new Chan<int>();
            c.Close();

            int val = c.Receive();
            Assert.AreEqual(default(int), val);
        }

        [Test]
        public void SelectSendsOnly()
        {
            var c1 = new Chan<int>();
            var c2 = new Chan<int>();

            var t1 = new Thread(() =>
                                {
                                    Thread.Sleep(1000);
                                    c1.Receive();
                                });
            t1.IsBackground = true;

            var t2 = new Thread(() => c2.Receive());
            t2.IsBackground = true;

            var list = new List<int>();

            t1.Start();
            t2.Start();

            Select
                .DebugName("SelectSendsOnly")
                .CaseSend("c1", c1, 1, () => list.Add(1))
                .CaseSend("c2", c2, 2, () => list.Add(2))
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(2, list[0], "list[0]");
        }

        [Test]
        public void SingleNumberGeneratorIEnumerableChan()
        {
            var c = new Chan<int>();

            var t = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    c.Send(i);
                }
                c.Close();
            });
            t.IsBackground = true;
            t.Start();

            var list = new List<int>();
            foreach (var i in (IEnumerable)c)
            {
                list.Add((int)i);
            }

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list);
        }

        [Test]
        public void SelectSendAndReceiveReceiveReady()
        {
            var c1 = new Chan<int>();
            var c2 = new Chan<int>();

            var t1 = new Thread(() =>
            {
                Thread.Sleep(10);
                c1.Send(1);
            });
            t1.IsBackground = true;

            var list = new List<int>();

            var t2 = new Thread(() => list.Add(c2.Receive()));
            t2.IsBackground = true;

            t1.Start();
            t2.Start();

            Select
                .DebugName("SelectSendAndReceiveReceiveReady")
                .CaseReceive("c1", c1, list.Add)
                .CaseSend("c2", c2, 2, () => { })
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(2, list[0], "list[0]");
        }

        [Test]
        public void SelectSendAndReceiveSendReady()
        {
            var c1 = new Chan<int>();
            var c2 = new Chan<int>();

            var t1 = new Thread(() => c1.Send(1));
            t1.IsBackground = true;

            var list = new List<int>();

            var t2 = new Thread(() =>
                                {
                                    Thread.Sleep(50);
                                    list.Add(c2.Receive());
                                });
            t2.IsBackground = true;

            t1.Start();
            t2.Start();

            Select
                .DebugName("SelectSendAndReceiveSendReady")
                .CaseReceive("c1", c1, list.Add)
                .CaseSend("c2", c2, 2, () => { })
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(1, list[0], "list[0]");
        }

        [Test]
        public void TwoSelectsSendAndReceiveCanTalk()
        {
            var c = new Chan<int>();

            int actual = 0;

            var wg = new WaitGroup();
            wg.Add(2);

            GoFunc.Run(() =>
            {
                Select
                    .DebugName("TwoSelectsSendAndReceiveCanTalk.Send")
                    .CaseSend("c", c, 7)
                    .NoDefault();

                wg.Done();
            });

            GoFunc.Run(() =>
            {
                Select
                    .DebugName("TwoSelectsSendAndReceiveCanTalk.Receive")
                    .CaseReceive("c", c, o => actual = o)
                    .NoDefault();

                wg.Done();
            });

            wg.Wait();

            Assert.AreEqual(7, actual);
        }

        [Test]
        public void BufferedChannelsDontBlock()
        {
            var c = new Chan<int>(1);
            c.Send(2);
            int actual = c.Receive();

            Assert.AreEqual(2, actual);
        }

        [Test]
        public void BufferedChannelsSelectSendAndReceiveInGoroutine()
        {
            var c = new Chan<int>(10);

            var list = new List<int>();
            var wg = new WaitGroup();

            wg.Add(2);

            GoFunc.Run(() =>
            {
                bool doLoop = true;
                while (doLoop)
                {
                    Select
                        .CaseReceiveOk(c, (i, ok) =>
                        {
                            if (ok)
                                list.Add(i);
                            else
                                doLoop = false;
                        })
                        .NoDefault();
                }

                wg.Done();
            }, "bufferChannelsTest:receiveLoop");

            GoFunc.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    Select
                        .CaseSend(c, i)
                        .NoDefault();
                }

                c.Close();
                wg.Done();
            }, "bufferedChannelsTest:sendLoop");

            wg.Wait();

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list.ToArray());
        }

        [Test]
        public void BufferedChannelsSelectSendInGoroutine()
        {
            var c = new Chan<int>(10);

            var list = new List<int>();
            var wg = new WaitGroup();

            wg.Add(1);

            GoFunc.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    Select
                        .CaseSend(c, i)
                        .NoDefault();
                }

                c.Close();
                wg.Done();
            }, "bufferedChannelsTest:sendLoop");

            wg.Wait();

            bool doLoop = true;
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (doLoop)
            {
                Select
                    .CaseReceiveOk(c, (i, ok) =>
                    {
                        if (ok)
                            list.Add(i);
                        else
                            doLoop = false;
                    })
                    .NoDefault();
            }

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list.ToArray());
        }

        [Test]
        public void BufferedChannelsSelectReceiveInGoroutine()
        {
            var c = new Chan<int>(10);

            var list = new List<int>();
            var wg = new WaitGroup();

            wg.Add(1);

            GoFunc.Run(() =>
            {
                bool doLoop = true;
                while (doLoop)
                {
                    Select
                        .CaseReceiveOk(c, (i, ok) =>
                        {
                            if (ok)
                                list.Add(i);
                            else
                                doLoop = false;
                        })
                        .NoDefault();
                }

                wg.Done();
            }, "bufferChannelsTest:receiveLoop");

            for (int i = 0; i < 10; i++)
            {
                Select
                    .CaseSend(c, i)
                    .NoDefault();
            }

            c.Close();

            wg.Wait();

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list.ToArray());
        }

        [Test]
        public void BufferedChannelsReceiveSelectInGoroutineSendOnMainThread()
        {
            var c = new Chan<int>(10);

            var list = new List<int>();
            var wg = new WaitGroup();

            wg.Add(1);

            GoFunc.Run(() =>
            {
                bool doLoop = true;
                while (doLoop)
                {
                    Select
                        .CaseReceiveOk(c, (i, ok) =>
                        {
                            if (ok)
                                list.Add(i);
                            else
                                doLoop = false;
                        })
                        .NoDefault();
                }

                wg.Done();
            }, "bufferChannelsTest:receiveLoop");

            for (int i = 0; i < 10; i++)
            {
                c.Send(i);
            }

            c.Close();

            wg.Wait();

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list.ToArray());
        }

        [Test]
        public void ClosedChannelsWithDataShouldNotReportClosedUntilDrained()
        {
            for (int i = 0; i < 10000; i++)
            {
                var chan = new Chan<int>();

                var wait = new AutoResetEvent(initialState: false);

                GoFunc.Run(() =>
                           {
                               chan.Send(1);
                               chan.Close();

                           }, "send");

                bool ok = false, ok2 = false;
                int actual = -1, actual2 = -1;

                GoFunc.Run(() =>
                           {
                               actual = chan.ReceiveOk(out ok);
                               actual2 = chan.ReceiveOk(out ok2);
                               wait.Set();
                           }, "receive");

                wait.WaitOne();

                Assert.AreEqual(true, ok, string.Format("ok iteration {0}", i));
                Assert.AreEqual(1, actual, string.Format("actual iteration {0}", i));
                Assert.AreEqual(false, ok2, string.Format("ok2 iteration {0}", i));
                Assert.AreEqual(default(int), actual2, string.Format("actual2 iteration {0}", i));
            }
        }
    }
}
