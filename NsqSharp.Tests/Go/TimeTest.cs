using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NsqSharp.Channels;
using NsqSharp.Go;
using NsqSharp.Tests.Utils;
using NUnit.Framework;

namespace NsqSharp.Tests.Go
{
    [TestFixture]
    public class TimeTest
    {
        private static readonly TestData<string, long> _parseDurationTests = new TestData<string, long>
        {
            // simple
            {"0", new Result<long>(0)},
            {"5s", new Result<long>(5 * Time.Second)},
            {"30s", new Result<long>(30 * Time.Second)},
            {"1478s", new Result<long>(1478 * Time.Second)},
            // sign
            {"-5s", new Result<long>(-5 * Time.Second)},
            {"+5s", new Result<long>(5 * Time.Second)},
            {"-0", new Result<long>(0)},
            {"+0", new Result<long>(0)},
            // decimal
            {"5.0s", new Result<long>(5 * Time.Second)},
            {"5.6s", new Result<long>(5 * Time.Second + 600 * Time.Millisecond)},
            {"5.s", new Result<long>(5 * Time.Second)},
            {".5s", new Result<long>(500 * Time.Millisecond)},
            {"1.0s", new Result<long>(1 * Time.Second)},
            {"1.00s", new Result<long>(1 * Time.Second)},
            {"1.004s", new Result<long>(1 * Time.Second + 4 * Time.Millisecond)},
            {"1.0040s", new Result<long>(1 * Time.Second + 4 * Time.Millisecond)},
            {"100.00100s", new Result<long>(100 * Time.Second + 1 * Time.Millisecond)},
            // different units
            {"10ns", new Result<long>(10 * Time.Nanosecond)},
            {"11us", new Result<long>(11 * Time.Microsecond)},
            {"12µs", new Result<long>(12 * Time.Microsecond)}, // U+00B5
            {"12μs", new Result<long>(12 * Time.Microsecond)}, // U+03BC
            {"13ms", new Result<long>(13 * Time.Millisecond)},
            {"14s", new Result<long>(14 * Time.Second)},
            {"15m", new Result<long>(15 * Time.Minute)},
            {"16h", new Result<long>(16 * Time.Hour)},
            // composite durations
            {"3h30m", new Result<long>(3 * Time.Hour + 30 * Time.Minute)},
            {"10.5s4m", new Result<long>(4 * Time.Minute + 10 * Time.Second + 500 * Time.Millisecond)},
            {"-2m3.4s", new Result<long>(-(2 * Time.Minute + 3 * Time.Second + 400 * Time.Millisecond))},
            {"1h2m3s4ms5us6ns", new Result<long>(1 * Time.Hour + 2 * Time.Minute + 3 * Time.Second + 4 * Time.Millisecond + 
                                                 5 * Time.Microsecond + 6 * Time.Nanosecond)},
            {"39h9m14.425s", new Result<long>(39 * Time.Hour + 9 * Time.Minute + 14 * Time.Second + 425 * Time.Millisecond)},
            // large value
            {"52763797000ns", new Result<long>(52763797000 * Time.Nanosecond)},
            // more than 9 digits after decimal point, see http://golang.org/issue/6617
            {"0.3333333333333333333h", new Result<long>(20 * Time.Minute)},

            // errors
            {"", new Result<long, InvalidDataException>()},
            {"3", new Result<long, InvalidDataException>()},
            {"-", new Result<long, InvalidDataException>()},
            {"s", new Result<long, InvalidDataException>()},
            {".", new Result<long, InvalidDataException>()},
            {"-.", new Result<long, InvalidDataException>()},
            {".s", new Result<long, InvalidDataException>()},
            {"+.s", new Result<long, InvalidDataException>()},
            {"3000000h", new Result<long, OverflowException>()}, // overflow
            {"5.6x", new Result<long, InvalidDataException>()}, // bad unit
            {"9223372036854775806ns", new Result<long, OverflowException>()}, // overflow in leadingInt
        };

        [Test]
        public void TestParseDuration()
        {
            foreach (var kvp in _parseDurationTests)
            {
                string input = kvp.Key;
                bool shouldPass = kvp.Value.ShouldPass;
                long expected = kvp.Value.Expected;
                Type expectedException = kvp.Value.ExpectedException;

                if (shouldPass)
                {
                    long actual = Time.ParseDuration(input);
                    Assert.AreEqual(expected, actual, input);
                }
                else
                {
                    Assert.Throws(expectedException, () => Time.ParseDuration(input));
                }
            }

            Assert.Throws<ArgumentNullException>(() => Time.ParseDuration(null));
        }

        [Test]
        public void AfterNotFired()
        {
            var c1 = Time.After(TimeSpan.FromMilliseconds(10));
            var c2 = new Chan<string>();

            var t1 = new Thread(() => c2.Send("no-timeout"));
            t1.IsBackground = true;
            t1.Start();

            var list = new List<string>();

            Select
                .CaseReceive(c1, o => list.Add("timeout"))
                .CaseReceive(c2, list.Add)
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual("no-timeout", list[0], "list[0]");
        }

        [Test]
        public void AfterFired()
        {
            var c1 = Time.After(TimeSpan.FromMilliseconds(10));
            var c2 = new Chan<string>();

            var t1 = new Thread(() =>
                                {
                                    Thread.Sleep(30);
                                    c2.Send("no-timeout");
                                });
            t1.IsBackground = true;
            t1.Start();

            var list = new List<string>();

            Select
                .CaseReceive(c1, o => list.Add("timeout"))
                .CaseReceive(c2, list.Add)
                .NoDefault();

            Assert.AreEqual(1, list.Count, "list.Count");
            Assert.AreEqual("timeout", list[0], "list[0]");
        }
    }
}
