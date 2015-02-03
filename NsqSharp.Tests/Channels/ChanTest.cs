using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NsqSharp.Channels;
using NUnit.Framework;

namespace NsqSharp.Tests.Channels
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
                                                               });

            var ch = new Chan<int>();

            var threadGenerate = new Thread(() => generate(ch));
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
                .CaseReceive(c1, list.Add)
                .CaseReceive(c2, list.Add)
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(2, list[0], "list[0]");
        }

        [Test]
        public void SelectNullChannel()
        {
            var c1 = new Chan<int>();
            var c2 = (Chan<int>)null;

            var t1 = new Thread(() =>
            {
                Thread.Sleep(10);
                c1.Send(3);
            });
            t1.IsBackground = true;

            var list = new List<int>();

            t1.Start();

            Select
                .CaseReceive(c2, list.Add)
                .CaseReceive(c1, list.Add)
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
                .CaseReceive(c1, list.Add)
                .CaseReceive(c2, list.Add)
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
                Thread.Sleep(10);
                c1.Send(1);
            });
            t1.IsBackground = true;

            var t2 = new Thread(() => c2.Send(5));
            t2.IsBackground = true;

            var list = new List<int>();

            t1.Start();
            t2.Start();

            Select
                .CaseReceive(c1, list.Add)
                .CaseReceive(c2, list.Add)
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
        public void ReceiveOnClosedChannelThrows()
        {
            var c = new Chan<int>();
            c.Close();

            Assert.Throws<ChannelClosedException>(() => c.Receive());
        }

        [Test]
        public void SelectSendsOnly()
        {
            var c1 = new Chan<int>();
            var c2 = new Chan<int>();

            var t1 = new Thread(() =>
                                {
                                    Thread.Sleep(10);
                                    c1.Receive();
                                });
            t1.IsBackground = true;

            var t2 = new Thread(() => c2.Receive());
            t2.IsBackground = true;

            var list = new List<int>();

            t1.Start();
            t2.Start();

            Select
                .CaseSend(c1, 1, () => list.Add(1))
                .CaseSend(c2, 2, () => list.Add(2))
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
                .CaseReceive(c1, list.Add)
                .CaseSend(c2, 2, () => { })
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
                                    Thread.Sleep(10);
                                    list.Add(c2.Receive());
                                });
            t2.IsBackground = true;

            t1.Start();
            t2.Start();

            Select
                .CaseReceive(c1, list.Add)
                .CaseSend(c2, 2, () => { })
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual(1, list[0], "list[0]");
        }
    }
}
