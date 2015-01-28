using System;
using NsqSharp.Go;
using NUnit.Framework;

namespace NsqSharp.Tests.Go
{
    [TestFixture]
    public class TimeTest
    {
        private static readonly TestData<string, long> _parseDurationTests = new TestData<string, long>
        {
	        // simple
	        {"0", new Result<long>(true, 0)},
	        {"5s", new Result<long>(true, 5 * Time.Second)},
	        {"30s", new Result<long>(true, 30 * Time.Second)},
	        {"1478s", new Result<long>(true, 1478 * Time.Second)},
	        // sign
	        {"-5s", new Result<long>(true, -5 * Time.Second)},
	        {"+5s", new Result<long>(true, 5 * Time.Second)},
	        {"-0", new Result<long>(true, 0)},
	        {"+0", new Result<long>(true, 0)},
	        // decimal
	        {"5.0s", new Result<long>(true, 5 * Time.Second)},
	        {"5.6s", new Result<long>(true, 5*Time.Second + 600*Time.Millisecond)},
	        {"5.s", new Result<long>(true, 5 * Time.Second)},
	        {".5s", new Result<long>(true, 500 * Time.Millisecond)},
	        {"1.0s", new Result<long>(true, 1 * Time.Second)},
	        {"1.00s", new Result<long>(true, 1 * Time.Second)},
	        {"1.004s", new Result<long>(true, 1*Time.Second + 4*Time.Millisecond)},
	        {"1.0040s", new Result<long>(true, 1*Time.Second + 4*Time.Millisecond)},
	        {"100.00100s", new Result<long>(true, 100*Time.Second + 1*Time.Millisecond)},
	        // different units
	        {"10ns", new Result<long>(true, 10 * Time.Nanosecond)},
	        {"11us", new Result<long>(true, 11 * Time.Microsecond)},
	        {"12µs", new Result<long>(true, 12 * Time.Microsecond)}, // U+00B5
	        {"12μs", new Result<long>(true, 12 * Time.Microsecond)}, // U+03BC
	        {"13ms", new Result<long>(true, 13 * Time.Millisecond)},
	        {"14s", new Result<long>(true, 14 * Time.Second)},
	        {"15m", new Result<long>(true, 15 * Time.Minute)},
	        {"16h", new Result<long>(true, 16 * Time.Hour)},
	        // composite durations
	        {"3h30m", new Result<long>(true, 3*Time.Hour + 30*Time.Minute)},
	        {"10.5s4m", new Result<long>(true, 4*Time.Minute + 10*Time.Second + 500*Time.Millisecond)},
	        {"-2m3.4s", new Result<long>(true, -(2*Time.Minute + 3*Time.Second + 400*Time.Millisecond))},
	        {"1h2m3s4ms5us6ns", new Result<long>(true, 1*Time.Hour + 2*Time.Minute + 3*Time.Second + 4*Time.Millisecond + 
                                                            5*Time.Microsecond + 6*Time.Nanosecond)},
	        {"39h9m14.425s", new Result<long>(true, 39*Time.Hour + 9*Time.Minute + 14*Time.Second + 425*Time.Millisecond)},
	        // large value
	        {"52763797000ns", new Result<long>(true, 52763797000 * Time.Nanosecond)},
	        // more than 9 digits after decimal point, see http://golang.org/issue/6617
	        {"0.3333333333333333333h", new Result<long>(true, 20 * Time.Minute)},

	        // errors
	        {"", new Result<long>(false, 0)},
	        {"3", new Result<long>(false, 0)},
	        {"-", new Result<long>(false, 0)},
	        {"s", new Result<long>(false, 0)},
	        {".", new Result<long>(false, 0)},
	        {"-.", new Result<long>(false, 0)},
	        {".s", new Result<long>(false, 0)},
	        {"+.s", new Result<long>(false, 0)},
	        {"3000000h", new Result<long>(false, 0)}, // overflow
            {"5.6x", new Result<long>(false, 0)}, // bad unit
            {"9223372036854775806ns", new Result<long>(false, 0)}, // overflow in leadingInt
        };

        [Test]
        public void TestParseDuration()
        {
            foreach (var kvp in _parseDurationTests)
            {
                string input = kvp.Key;
                bool shouldPass = kvp.Value.ShouldPass;
                long expected = kvp.Value.Expected;

                long actual = 0;
                try
                {
                    actual = Time.ParseDuration(input);
                    if (!shouldPass)
                        Assert.Fail(string.Format("Exception expected for '{0}'", input));
                }
                catch (Exception ex)
                {
                    if (shouldPass)
                    {
                        Assert.Fail(string.Format("Exception occurred for '{0}' {1}", input, ex.Message));
                    }
                }

                Assert.AreEqual(expected, actual, input);
            }
        }
    }
}
