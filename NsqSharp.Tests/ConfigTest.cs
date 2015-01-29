using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using NsqSharp.Attributes;
using NsqSharp.Extensions;
using NsqSharp.Go;
using NUnit.Framework;

namespace NsqSharp.Tests
{
    [TestFixture]
    public class ConfigTest
    {
        [Test]
        public void TestConfigSet()
        {
            var c = new Config();

            Assert.Throws<Exception>(() => c.Set("not a real config value", new object()),
                "No error when setting an invalid value");

            Assert.Throws<Exception>(() => c.Set("tls_v1", "lol"),
                "No error when setting `tls_v1` to an invalid value");

            c.Set("tls_v1", true);
            Assert.IsTrue(c.TlsV1, "Error setting `tls_v1` config.");

            c.Set("tls-insecure-skip-verify", true);
            Assert.IsTrue(c.TlsConfig.InsecureSkipVerify);

            c.Set("tls-min-version", "tls1.2");

            Assert.Throws<Exception>(() => c.Set("tls-min-version", "tls1.3"),
                "No error when setting `tls-min-version` to an invalid value");
        }

        [Test]
        public void TestConfigValidateDefault()
        {
            var c = new Config();
            c.Validate();
        }

        [Test]
        public void TestConfigValidateError()
        {
            var c = new Config();

            Assert.Throws<Exception>(() => c.Set("deflate_level", 100));

            // property wasn't set, state should still be ok
            c.Validate();

            c.DeflateLevel = 100;
            Assert.AreEqual(100, c.DeflateLevel);

            Assert.Throws<Exception>(c.Validate);

            c.DeflateLevel = 1;
            c.Validate();

            c.DeflateLevel = 0;
            Assert.AreEqual(0, c.DeflateLevel);
            Assert.Throws<Exception>(c.Validate);
        }

        [Test]
        public void TestOptNamesUnique()
        {
            var list = new List<string>();
            foreach (var propertyInfo in typeof(Config).GetProperties())
            {
                var opt = propertyInfo.Get<OptAttribute>();
                if (opt == null)
                    continue;

                string option = opt.Name;

                if (list.Contains(option))
                {
                    Assert.Fail(string.Format("property opt '{0}' exists more than once", option));
                }

                list.Add(option);
            }
        }

        [Test]
        public void TestOptNamesAreLowerAndTrimmed()
        {
            foreach (var propertyInfo in typeof(Config).GetProperties())
            {
                var opt = propertyInfo.Get<OptAttribute>();
                if (opt == null)
                    continue;

                if (opt.Name != opt.Name.ToLower().Trim() || opt.Name.Contains("-"))
                {
                    Assert.Fail(string.Format("property opt '{0}' does not match naming rules", opt.Name));
                }
            }
        }

        [Test]
        public void TestDefaultValues()
        {
            var c = new Config();

            Assert.AreEqual(TimeSpan.FromSeconds(60), c.ReadTimeout, "read_timeout");
            Assert.AreEqual(TimeSpan.FromSeconds(1), c.WriteTimeout, "write_timeout");
            Assert.AreEqual(TimeSpan.FromSeconds(60), c.LookupdPollInterval, "lookupd_poll_interval");
            Assert.AreEqual(0.3, c.LookupdPollJitter, "lookupd_poll_jitter");
            Assert.AreEqual(TimeSpan.FromMinutes(15), c.MaxRequeueDelay, "max_requeue_delay");
            Assert.AreEqual(TimeSpan.FromSeconds(90), c.DefaultRequeueDelay, "default_requeue_delay");
            Assert.AreEqual(TimeSpan.FromSeconds(1), c.BackoffMultiplier, "backoff_multiplier");
            Assert.AreEqual(5, c.MaxAttempts, "max_attempts");
            Assert.AreEqual(TimeSpan.FromSeconds(10), c.LowRdyIdleTimeout, "low_rdy_idle_timeout");
            Assert.AreEqual(Dns.GetHostName().Split(new[] { '.' })[0], c.ClientID, "client_id");
            Assert.AreEqual(Dns.GetHostName(), c.Hostname, "hostname");
            Assert.AreEqual("NsqSharp/0.0.2", c.UserAgent, "user_agent");
            Assert.AreEqual(TimeSpan.FromSeconds(30), c.HeartbeatInterval, "heartbeat_interval");
            Assert.AreEqual(0, c.SampleRate, "sample_rate");
            Assert.AreEqual(false, c.TlsV1, "tls_v1");
            Assert.IsNull(c.TlsConfig, "tls_config");
            Assert.AreEqual(false, c.Deflate, "deflate");
            Assert.AreEqual(6, c.DeflateLevel, "deflate_level");
            Assert.AreEqual(false, c.Snappy, "snappy");
            Assert.AreEqual(16384, c.OutputBufferSize, "output_buffer_size");
            Assert.AreEqual(TimeSpan.FromMilliseconds(250), c.OutputBufferTimeout, "output_buffer_timeout");
            Assert.AreEqual(1, c.MaxInFlight, "max_in_flight");
            Assert.AreEqual(TimeSpan.FromMinutes(2), c.MaxBackoffDuration, "max_backoff_duration");
            Assert.AreEqual(TimeSpan.Zero, c.MsgTimeout, "msg_timeout");
            Assert.IsNull(c.AuthSecret, "auth_secret");
        }

        [Test]
        public void TestMinValues()
        {
            var c = new Config();
            c.Set("read_timeout", TimeSpan.FromMilliseconds(100));
            c.Set("write_timeout", TimeSpan.FromMilliseconds(100));
            c.Set("lookupd_poll_interval", TimeSpan.FromSeconds(5));
            c.Set("lookupd_poll_jitter", 0);
            c.Set("max_requeue_delay", TimeSpan.Zero);
            c.Set("default_requeue_delay", TimeSpan.Zero);
            c.Set("backoff_multiplier", 0);
            c.Set("max_attempts", 0);
            c.Set("low_rdy_idle_timeout", TimeSpan.FromSeconds(1));
            c.Set("client_id", null);
            c.Set("hostname", null);
            c.Set("user_agent", null);
            c.Set("heartbeat_interval", TimeSpan.MinValue);
            c.Set("sample_rate", 0);
            c.Set("tls_v1", false);
            c.Set("tls_config", null);
            c.Set("deflate", false);
            c.Set("deflate_level", 1);
            c.Set("snappy", false);
            c.Set("output_buffer_size", Int64.MinValue);
            c.Set("output_buffer_timeout", TimeSpan.MinValue);
            c.Set("max_in_flight", 0);
            c.Set("max_backoff_duration", TimeSpan.Zero);
            c.Set("msg_timeout", TimeSpan.Zero);
            c.Set("auth_secret", null);

            Assert.AreEqual(TimeSpan.FromMilliseconds(100), c.ReadTimeout, "read_timeout");
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), c.WriteTimeout, "write_timeout");
            Assert.AreEqual(TimeSpan.FromSeconds(5), c.LookupdPollInterval, "lookupd_poll_interval");
            Assert.AreEqual(0, c.LookupdPollJitter, "lookupd_poll_jitter");
            Assert.AreEqual(TimeSpan.Zero, c.MaxRequeueDelay, "max_requeue_delay");
            Assert.AreEqual(TimeSpan.Zero, c.DefaultRequeueDelay, "default_requeue_delay");
            Assert.AreEqual(TimeSpan.Zero, c.BackoffMultiplier, "backoff_multiplier");
            Assert.AreEqual(0, c.MaxAttempts, "max_attempts");
            Assert.AreEqual(TimeSpan.FromSeconds(1), c.LowRdyIdleTimeout, "low_rdy_idle_timeout");
            Assert.AreEqual(null, c.ClientID, "client_id");
            Assert.AreEqual(null, c.Hostname, "hostname");
            Assert.AreEqual(null, c.UserAgent, "user_agent");
            Assert.AreEqual(TimeSpan.MinValue, c.HeartbeatInterval, "heartbeat_interval");
            Assert.AreEqual(0, c.SampleRate, "sample_rate");
            Assert.AreEqual(false, c.TlsV1, "tls_v1");
            Assert.AreEqual(null, c.TlsConfig, "tls_config");
            Assert.AreEqual(false, c.Deflate, "deflate");
            Assert.AreEqual(1, c.DeflateLevel, "deflate_level");
            Assert.AreEqual(false, c.Snappy, "snappy");
            Assert.AreEqual(Int64.MinValue, c.OutputBufferSize, "output_buffer_size");
            Assert.AreEqual(TimeSpan.MinValue, c.OutputBufferTimeout, "output_buffer_timeout");
            Assert.AreEqual(0, c.MaxInFlight, "max_in_flight");
            Assert.AreEqual(TimeSpan.Zero, c.MaxBackoffDuration, "max_backoff_duration");
            Assert.AreEqual(TimeSpan.Zero, c.MsgTimeout, "msg_timeout");
            Assert.AreEqual(null, c.AuthSecret, "auth_secret");
        }

        [Test]
        public void TestMaxValues()
        {
            var c = new Config();
            var tlsConfig = new TlsConfig();
            c.Set("read_timeout", TimeSpan.FromMinutes(5));
            c.Set("write_timeout", TimeSpan.FromMinutes(5));
            c.Set("lookupd_poll_interval", TimeSpan.FromMinutes(5));
            c.Set("lookupd_poll_jitter", 1);
            c.Set("max_requeue_delay", TimeSpan.FromMinutes(60));
            c.Set("default_requeue_delay", TimeSpan.FromMinutes(60));
            c.Set("backoff_multiplier", TimeSpan.FromMinutes(60));
            c.Set("max_attempts", 65535);
            c.Set("low_rdy_idle_timeout", TimeSpan.FromMinutes(5));
            c.Set("client_id", "my");
            c.Set("hostname", "my.host.name.com");
            c.Set("user_agent", "user-agent/1.0");
            c.Set("heartbeat_interval", TimeSpan.MaxValue);
            c.Set("sample_rate", 99);
            c.Set("tls_v1", true);
            c.Set("tls_config", tlsConfig);
            c.Set("deflate", true);
            c.Set("deflate_level", 9);
            c.Set("snappy", true);
            c.Set("output_buffer_size", Int64.MaxValue);
            c.Set("output_buffer_timeout", TimeSpan.MaxValue);
            c.Set("max_in_flight", int.MaxValue);
            c.Set("max_backoff_duration", TimeSpan.FromMinutes(60));
            c.Set("msg_timeout", TimeSpan.MaxValue);
            c.Set("auth_secret", "!@#@#$#%");

            Assert.AreEqual(TimeSpan.FromMinutes(5), c.ReadTimeout, "read_timeout");
            Assert.AreEqual(TimeSpan.FromMinutes(5), c.WriteTimeout, "write_timeout");
            Assert.AreEqual(TimeSpan.FromMinutes(5), c.LookupdPollInterval, "lookupd_poll_interval");
            Assert.AreEqual(1, c.LookupdPollJitter, "lookupd_poll_jitter");
            Assert.AreEqual(TimeSpan.FromMinutes(60), c.MaxRequeueDelay, "max_requeue_delay");
            Assert.AreEqual(TimeSpan.FromMinutes(60), c.DefaultRequeueDelay, "default_requeue_delay");
            Assert.AreEqual(TimeSpan.FromMinutes(60), c.BackoffMultiplier, "backoff_multiplier");
            Assert.AreEqual(65535, c.MaxAttempts, "max_attempts");
            Assert.AreEqual(TimeSpan.FromMinutes(5), c.LowRdyIdleTimeout, "low_rdy_idle_timeout");
            Assert.AreEqual("my", c.ClientID, "client_id");
            Assert.AreEqual("my.host.name.com", c.Hostname, "hostname");
            Assert.AreEqual("user-agent/1.0", c.UserAgent, "user_agent");
            Assert.AreEqual(TimeSpan.MaxValue, c.HeartbeatInterval, "heartbeat_interval");
            Assert.AreEqual(99, c.SampleRate, "sample_rate");
            Assert.AreEqual(true, c.TlsV1, "tls_v1");
            Assert.AreEqual(tlsConfig, c.TlsConfig, "tls_config");
            Assert.AreEqual(true, c.Deflate, "deflate");
            Assert.AreEqual(9, c.DeflateLevel, "deflate_level");
            Assert.AreEqual(true, c.Snappy, "snappy");
            Assert.AreEqual(Int64.MaxValue, c.OutputBufferSize, "output_buffer_size");
            Assert.AreEqual(TimeSpan.MaxValue, c.OutputBufferTimeout, "output_buffer_timeout");
            Assert.AreEqual(int.MaxValue, c.MaxInFlight, "max_in_flight");
            Assert.AreEqual(TimeSpan.FromMinutes(60), c.MaxBackoffDuration, "max_backoff_duration");
            Assert.AreEqual(TimeSpan.MaxValue, c.MsgTimeout, "msg_timeout");
            Assert.AreEqual("!@#@#$#%", c.AuthSecret, "auth_secret");
        }

        [Test]
        public void TestValidatesLessThanMinValues()
        {
            var c = new Config();
            var tick = new TimeSpan(1);

            Assert.Throws<Exception>(() => c.Set("read_timeout", TimeSpan.FromMilliseconds(100) - tick), "read_timeout");
            Assert.Throws<Exception>(() => c.Set("write_timeout", TimeSpan.FromMilliseconds(100) - tick), "write_timeout");
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_interval", TimeSpan.FromSeconds(5) - tick), "lookupd_poll_interval");
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_jitter", 0 - double.Epsilon), "lookupd_poll_jitter");
            Assert.Throws<Exception>(() => c.Set("max_requeue_delay", TimeSpan.Zero - tick), "max_requeue_delay");
            Assert.Throws<Exception>(() => c.Set("default_requeue_delay", TimeSpan.Zero - tick), "default_requeue_delay");
            Assert.Throws<Exception>(() => c.Set("backoff_multiplier", TimeSpan.Zero - tick), "backoff_multiplier");
            Assert.Throws<Exception>(() => c.Set("max_attempts", 0 - 1), "max_attempts");
            Assert.Throws<Exception>(() => c.Set("low_rdy_idle_timeout", TimeSpan.FromSeconds(1) - tick), "low_rdy_idle_timeout");
            //c.Set("client_id", null);
            //c.Set("hostname", null);
            //c.Set("user_agent", null);
            //Assert.Throws<Exception>(() => c.Set("heartbeat_interval", TimeSpan.MinValue - tick), "heartbeat_interval");
            Assert.Throws<Exception>(() => c.Set("sample_rate", 0 - 1), "sample_rate");
            //c.Set("tls_v1", false);
            //c.Set("tls_config", null);
            //c.Set("deflate", false);
            Assert.Throws<Exception>(() => c.Set("deflate_level", 1 - 1), "deflate_level");
            //c.Set("snappy", false);
            //Assert.Throws<Exception>(() => c.Set("output_buffer_size", Int64.MinValue - 1), "");
            //Assert.Throws<Exception>(() => c.Set("output_buffer_timeout", TimeSpan.MinValue - tick), "output_buffer_timeout");
            Assert.Throws<Exception>(() => c.Set("max_in_flight", 0 - 1), "max_in_flight");
            Assert.Throws<Exception>(() => c.Set("max_backoff_duration", TimeSpan.Zero - tick), "max_backoff_duration");
            Assert.Throws<Exception>(() => c.Set("msg_timeout", TimeSpan.Zero - tick), "msg_timeout");
            //Assert.Throws<Exception>(() => c.Set("auth_secret", null), "");
        }

        [Test]
        public void TestValidatesGreaterThanMaxValues()
        {
            var c = new Config();
            var tick = new TimeSpan(1);

            Assert.Throws<Exception>(() => c.Set("read_timeout", TimeSpan.FromMinutes(5) + tick), "read_timeout");
            Assert.Throws<Exception>(() => c.Set("write_timeout", TimeSpan.FromMinutes(5) + tick), "write_timeout");
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_interval", TimeSpan.FromMinutes(5) + tick), "lookupd_poll_interval");
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_jitter", 1 + 0.0001), "lookupd_poll_jitter");
            Assert.Throws<Exception>(() => c.Set("max_requeue_delay", TimeSpan.FromMinutes(60) + tick), "max_requeue_delay");
            Assert.Throws<Exception>(() => c.Set("default_requeue_delay", TimeSpan.FromMinutes(60) + tick), "default_requeue_delay");
            Assert.Throws<Exception>(() => c.Set("backoff_multiplier", TimeSpan.FromMinutes(60) + tick), "backoff_multiplier");
            Assert.Throws<Exception>(() => c.Set("max_attempts", 65535 + 1), "max_attempts");
            Assert.Throws<Exception>(() => c.Set("low_rdy_idle_timeout", TimeSpan.FromMinutes(5) + tick), "low_rdy_idle_timeout");
            //Assert.Throws<Exception>(() => c.Set("client_id", "my"), "client_id");
            //Assert.Throws<Exception>(() => c.Set("hostname", "my.host.name.com"), "hostname");
            //Assert.Throws<Exception>(() => c.Set("user_agent", "user-agent/1.0"), "user_agent");
            //Assert.Throws<Exception>(() => c.Set("heartbeat_interval", TimeSpan.MaxValue), "heartbeat_interval");
            Assert.Throws<Exception>(() => c.Set("sample_rate", 99 + 1), "sample_rate");
            //Assert.Throws<Exception>(() => c.Set("tls_v1", true), "tls_v1");
            //Assert.Throws<Exception>(() => c.Set("tls_config", tlsConfig);
            //Assert.Throws<Exception>(() => c.Set("deflate", true), "deflate");
            Assert.Throws<Exception>(() => c.Set("deflate_level", 9 + 1), "deflate_level");
            //Assert.Throws<Exception>(() => c.Set("snappy", true), "snappy");
            //Assert.Throws<Exception>(() => c.Set("output_buffer_size", Int64.MaxValue), "output_buffer_size");
            //Assert.Throws<Exception>(() => c.Set("output_buffer_timeout", TimeSpan.MaxValue), "output_buffer_timeout");
            //Assert.Throws<Exception>(() => c.Set("max_in_flight", int.MaxValue), "max_in_flight");
            Assert.Throws<Exception>(() => c.Set("max_backoff_duration", TimeSpan.FromMinutes(60) + tick), "max_backoff_duration");
            //Assert.Throws<Exception>(() => c.Set("msg_timeout", TimeSpan.MaxValue), "msg_timeout");
            //Assert.Throws<Exception>(() => c.Set("auth_secret", "!@#@#$#%"), "auth_secret");
        }

        [Test]
        public void TestHeartbeatLessThanReadTimout()
        {
            var c = new Config();

            c.Set("read_timeout", "5m");
            c.Set("heartbeat_interval", "2s");
            c.Validate();

            c.Set("read_timeout", "2s");
            c.Set("heartbeat_interval", "5m");
            Assert.Throws<Exception>(c.Validate);
        }

        [Test]
        public void TestTls()
        {
            // TODO: Test more TLS

            var c = new Config();
            c.Set("tls_insecure_skip_verify", true);
            
            Assert.IsNotNull(c.TlsConfig, "TlsConfig");
            Assert.IsTrue(c.TlsConfig.InsecureSkipVerify, "TlsConfig.InsecureSkipVerify");

            c.Set("tls_min_version", "ssl3.0");
            Assert.AreEqual(SslProtocols.Ssl3, c.TlsConfig.MinVersion);

            c.Set("tls_min_version", "tls1.0");
            Assert.AreEqual(SslProtocols.Tls, c.TlsConfig.MinVersion);

            c.Set("tls_min_version", "tls1.1");
            Assert.AreEqual(SslProtocols.Tls11, c.TlsConfig.MinVersion);

            c.Set("tls_min_version", "tls1.2");
            Assert.AreEqual(SslProtocols.Tls12, c.TlsConfig.MinVersion);

            Assert.Throws<Exception>(() => c.Set("tls_min_version", "ssl2.0"));
        }
    }
}
