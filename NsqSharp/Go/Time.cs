using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NsqSharp.Channels;

namespace NsqSharp.Go
{
    // http://golang.org/src/time/time.go
    // http://golang.org/src/time/format.go

    /// <summary>
    /// Package time provides functionality for measuring and displaying time.
    /// </summary>
    public static class Time
    {
        /// <summary>Nanosecond</summary>
        public const long Nanosecond = 1;
        /// <summary>Microsecond</summary>
        public const long Microsecond = Nanosecond * 1000;
        /// <summary>Millisecond</summary>
        public const long Millisecond = Microsecond * 1000;
        /// <summary>Second</summary>
        public const long Second = Millisecond * 1000;
        /// <summary>Minute</summary>
        public const long Minute = Second * 60;
        /// <summary>Hour</summary>
        public const long Hour = Minute * 60;

        /// <summary>
        /// Creates a channel which fires after the specified timeout.
        /// </summary>
        public static Chan<bool> After(TimeSpan timeout)
        {
            var fireAt = DateTime.UtcNow + timeout;

            var timeoutChan = new Chan<bool>();

            Task.Factory.StartNew(() =>
                                  {
                                      var sleep = (fireAt - DateTime.UtcNow);
                                      if (sleep > TimeSpan.Zero)
                                      {
                                          Task.Delay(sleep).Wait();
                                      }

                                      timeoutChan.Send(default(bool));
                                  });

            return timeoutChan;
        }

        /// <summary>
        /// leadingInt consumes the leading [0-9]* from s.
        /// </summary>
        private static long leadingInt(ref Slice<char> s)
        {
            int i = 0;
            long x = 0;
            for (; i < s.Len(); i++)
            {
                char c = s[i];
                if (c < '0' || c > '9')
                {
                    break;
                }
                if (x >= (long.MaxValue - 10) / 10)
                {
                    // overflow
                    throw new OverflowException(s.ToString());
                }
                x = x * 10 + (c - '0');
            }
            s = s.Slc(i);
            return x;
        }

        private static readonly Dictionary<string, double> _unitMap = new Dictionary<string, double>
                                                                     {
                                                                         {"ns", Nanosecond},
                                                                         {"us", Microsecond},
                                                                         // U+00B5 = micro symbol
                                                                         {"µs", Microsecond},
                                                                         // U+03BC = Greek letter mu
                                                                         {"μs", Microsecond},
                                                                         {"ms", Millisecond},
                                                                         {"s", Second},
                                                                         {"m", Minute},
                                                                         {"h", Hour},
                                                                     };

        /// <summary>
        /// ParseDuration parses a duration string.
        /// A duration string is a possibly signed sequence of
        /// decimal numbers, each with optional fraction and a unit suffix,
        /// such as "300ms", "-1.5h" or "2h45m".
        /// Valid time units are "ns", "us" (or "µs"), "ms", "s", "m", "h".
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <returns>The parsed duration.</returns>
        public static long ParseDuration(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            // [-+]?([0-9]*(\.[0-9]*)?[a-z]+)+
            string orig = value;
            long f = 0;
            bool neg = false;
            Slice<char> s = new Slice<char>(value);

            // Consume [-+]?
            if (s != "")
            {
                var c = s[0];
                if (c == '-' || c == '+')
                {
                    neg = (c == '-');
                    s = s.Slc(1);
                }
            }

            // Special case: if all that is left is "0", this is zero.
            if (s == "0")
            {
                return 0;
            }

            if (s == "")
            {
                throw new InvalidDataException("time: invalid duration " + orig);
            }

            while (s != "")
            {
                // The next character must be [0-9.]
                if (!(s[0] == '.' || ('0' <= s[0] && s[0] <= '9')))
                {
                    throw new InvalidDataException("time: invalid duration " + orig);
                }

                // Consume [0-9]*
                var pl1 = s.Len();
                long x = leadingInt(ref s);

                double g = x;
                bool pre = (pl1 != s.Len()); // whether we consumed anything before a period

                // Consume (\.[0-9]*)?
                bool post = false;
                if (s != "" && s[0] == '.')
                {
                    s = s.Slc(1);
                    int pl2 = s.Len();
                    x = leadingInt(ref s);
                    double scale = 1.0;
                    for (var n = pl2 - s.Len(); n > 0; n--)
                    {
                        scale *= 10;
                    }
                    g += x / scale;
                    post = (pl2 != s.Len());
                }
                if (!pre && !post)
                {
                    // no digits (e.g. ".s" or "-.s")
                    throw new InvalidDataException("time: invalid duration " + orig);
                }

                // Consume unit.
                int i = 0;
                for (; i < s.Len(); i++)
                {
                    char c = s[i];
                    if (c == '.' || ('0' <= c && c <= '9'))
                    {
                        break;
                    }
                }
                if (i == 0)
                {
                    throw new InvalidDataException("time: missing unit in duration " + orig);
                }
                var u = s.Slc(0, i);
                s = s.Slc(i);

                double unit;
                bool ok = _unitMap.TryGetValue(u.ToString(), out unit);
                if (!ok)
                {
                    throw new InvalidDataException("time: unknown unit " + u + " in duration " + orig);
                }

                checked
                {
                    f += (long)(g * unit);
                }
            }

            if (neg)
            {
                f = -f;
            }

            return f;
        }
    }
}
