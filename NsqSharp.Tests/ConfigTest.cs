using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using NsqSharp.Attributes;
using NsqSharp.Extensions;
using NsqSharp.Go;
using Xunit;

namespace NsqSharp.Tests
{
    public class ConfigTest
    {
        [Fact]
        public void TestConfigSet()
        {
            var c = new Config();

            Assert.Throws<Exception>(() => c.Set("not a real config value", new object()));

            Assert.Throws<Exception>(() => c.Set("tls_v1", "lol"));

            c.Set("tls_v1", true);
            Assert.True(c.TlsV1, "Error setting `tls_v1` config.");

            c.Set("tls-insecure-skip-verify", true);
            Assert.True(c.TlsConfig.InsecureSkipVerify);

            c.Set("tls-min-version", "tls1.2");

            Assert.Throws<Exception>(() => c.Set("tls-min-version", "tls1.3"));
        }

        [Fact]
        public void TestConfigValidateDefault()
        {
            var c = new Config();
            c.Validate();
        }

        [Fact]
        public void TestConfigValidateError()
        {
            var c = new Config();

            Assert.Throws<Exception>(() => c.Set("deflate_level", 100));

            // property wasn't set, state should still be ok
            c.Validate();

            c.DeflateLevel = 100;
            Assert.Equal(100, c.DeflateLevel);

            Assert.Throws<Exception>(() => c.Validate());

            c.DeflateLevel = 1;
            c.Validate();

            c.DeflateLevel = 0;
            Assert.Equal(0, c.DeflateLevel);
            Assert.Throws<Exception>(() => c.Validate());
        }

        [Fact]
        public void TestOptNamesUnique()
        {
            var list = new List<string>();
            foreach (var propertyInfo in typeof(Config).GetProperties())
            {
                var opt = propertyInfo.Get<OptAttribute>();

                string option = opt.Name;

                Assert.DoesNotContain(option, list);
                list.Add(option);
            }
        }

        [Fact]
        public void TestOptNamesAreLowerAndTrimmed()
        {
            foreach (var propertyInfo in typeof(Config).GetProperties())
            {
                var opt = propertyInfo.Get<OptAttribute>();

                bool isInvalidOptName = (opt.Name != opt.Name.ToLower().Trim() || opt.Name.Contains("-"));
                Assert.False(isInvalidOptName, string.Format("property opt '{0}' does not match naming rules", opt.Name));
            }
        }

        [Fact]
        public void TestDefaultValues()
        {
            var c = new Config();

            Assert.Equal(TimeSpan.FromSeconds(60), c.ReadTimeout);
            Assert.Equal(TimeSpan.FromSeconds(1), c.WriteTimeout);
            Assert.Equal(TimeSpan.FromSeconds(60), c.LookupdPollInterval);
            Assert.Equal(0.3, c.LookupdPollJitter);
            Assert.Equal(TimeSpan.FromMinutes(15), c.MaxRequeueDelay);
            Assert.Equal(TimeSpan.FromSeconds(90), c.DefaultRequeueDelay);
            Assert.Equal(TimeSpan.FromSeconds(1), c.BackoffMultiplier);
            Assert.Equal(5, c.MaxAttempts);
            Assert.Equal(TimeSpan.FromSeconds(10), c.LowRdyIdleTimeout);
            Assert.Equal(Dns.GetHostName().Split(new[] { '.' })[0], c.ClientID);
            Assert.Equal(Dns.GetHostName(), c.Hostname);
            Assert.Equal("NsqSharp/0.0.2", c.UserAgent);
            Assert.Equal(TimeSpan.FromSeconds(30), c.HeartbeatInterval);
            Assert.Equal(0, c.SampleRate);
            Assert.Equal(false, c.TlsV1);
            Assert.Equal(null, c.TlsConfig);
            Assert.Equal(false, c.Deflate);
            Assert.Equal(6, c.DeflateLevel);
            Assert.Equal(false, c.Snappy);
            Assert.Equal(16384, c.OutputBufferSize);
            Assert.Equal(TimeSpan.FromMilliseconds(250), c.OutputBufferTimeout);
            Assert.Equal(1, c.MaxInFlight);
            Assert.Equal(TimeSpan.FromMinutes(2), c.MaxBackoffDuration);
            Assert.Equal(TimeSpan.Zero, c.MsgTimeout);
            Assert.Equal(null, c.AuthSecret);
        }

        [Fact]
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

            Assert.Equal(TimeSpan.FromMilliseconds(100), c.ReadTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(100), c.WriteTimeout);
            Assert.Equal(TimeSpan.FromSeconds(5), c.LookupdPollInterval);
            Assert.Equal(0, c.LookupdPollJitter);
            Assert.Equal(TimeSpan.Zero, c.MaxRequeueDelay);
            Assert.Equal(TimeSpan.Zero, c.DefaultRequeueDelay);
            Assert.Equal(TimeSpan.Zero, c.BackoffMultiplier);
            Assert.Equal(0, c.MaxAttempts);
            Assert.Equal(TimeSpan.FromSeconds(1), c.LowRdyIdleTimeout);
            Assert.Equal(null, c.ClientID);
            Assert.Equal(null, c.Hostname);
            Assert.Equal(null, c.UserAgent);
            Assert.Equal(TimeSpan.MinValue, c.HeartbeatInterval);
            Assert.Equal(0, c.SampleRate);
            Assert.Equal(false, c.TlsV1);
            Assert.Equal(null, c.TlsConfig);
            Assert.Equal(false, c.Deflate);
            Assert.Equal(1, c.DeflateLevel);
            Assert.Equal(false, c.Snappy);
            Assert.Equal(Int64.MinValue, c.OutputBufferSize);
            Assert.Equal(TimeSpan.MinValue, c.OutputBufferTimeout);
            Assert.Equal(0, c.MaxInFlight);
            Assert.Equal(TimeSpan.Zero, c.MaxBackoffDuration);
            Assert.Equal(TimeSpan.Zero, c.MsgTimeout);
            Assert.Equal(null, c.AuthSecret);
        }

        [Fact]
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

            Assert.Equal(TimeSpan.FromMinutes(5), c.ReadTimeout);
            Assert.Equal(TimeSpan.FromMinutes(5), c.WriteTimeout);
            Assert.Equal(TimeSpan.FromMinutes(5), c.LookupdPollInterval);
            Assert.Equal(1, c.LookupdPollJitter);
            Assert.Equal(TimeSpan.FromMinutes(60), c.MaxRequeueDelay);
            Assert.Equal(TimeSpan.FromMinutes(60), c.DefaultRequeueDelay);
            Assert.Equal(TimeSpan.FromMinutes(60), c.BackoffMultiplier);
            Assert.Equal(65535, c.MaxAttempts);
            Assert.Equal(TimeSpan.FromMinutes(5), c.LowRdyIdleTimeout);
            Assert.Equal("my", c.ClientID);
            Assert.Equal("my.host.name.com", c.Hostname);
            Assert.Equal("user-agent/1.0", c.UserAgent);
            Assert.Equal(TimeSpan.MaxValue, c.HeartbeatInterval);
            Assert.Equal(99, c.SampleRate);
            Assert.Equal(true, c.TlsV1);
            Assert.Equal(tlsConfig, c.TlsConfig);
            Assert.Equal(true, c.Deflate);
            Assert.Equal(9, c.DeflateLevel);
            Assert.Equal(true, c.Snappy);
            Assert.Equal(Int64.MaxValue, c.OutputBufferSize);
            Assert.Equal(TimeSpan.MaxValue, c.OutputBufferTimeout);
            Assert.Equal(int.MaxValue, c.MaxInFlight);
            Assert.Equal(TimeSpan.FromMinutes(60), c.MaxBackoffDuration);
            Assert.Equal(TimeSpan.MaxValue, c.MsgTimeout);
            Assert.Equal("!@#@#$#%", c.AuthSecret);
        }

        [Fact]
        public void TestValidatesLessThanMinValues()
        {
            var c = new Config();
            var tick = new TimeSpan(1);

            Assert.Throws<Exception>(() => c.Set("read_timeout", TimeSpan.FromMilliseconds(100) - tick));
            Assert.Throws<Exception>(() => c.Set("write_timeout", TimeSpan.FromMilliseconds(100) - tick));
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_interval", TimeSpan.FromSeconds(5) - tick));
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_jitter", 0 - double.Epsilon));
            Assert.Throws<Exception>(() => c.Set("max_requeue_delay", TimeSpan.Zero - tick));
            Assert.Throws<Exception>(() => c.Set("default_requeue_delay", TimeSpan.Zero - tick));
            Assert.Throws<Exception>(() => c.Set("backoff_multiplier", TimeSpan.Zero - tick));
            Assert.Throws<Exception>(() => c.Set("max_attempts", 0 - 1));
            Assert.Throws<Exception>(() => c.Set("low_rdy_idle_timeout", TimeSpan.FromSeconds(1) - tick));
            //c.Set("client_id", null);
            //c.Set("hostname", null);
            //c.Set("user_agent", null);
            //Assert.Throws<Exception>(() => c.Set("heartbeat_interval", TimeSpan.MinValue - tick));
            Assert.Throws<Exception>(() => c.Set("sample_rate", 0 - 1));
            //c.Set("tls_v1", false);
            //c.Set("tls_config", null);
            //c.Set("deflate", false);
            Assert.Throws<Exception>(() => c.Set("deflate_level", 1 - 1));
            //c.Set("snappy", false);
            //Assert.Throws<Exception>(() => c.Set("output_buffer_size", Int64.MinValue - 1));
            //Assert.Throws<Exception>(() => c.Set("output_buffer_timeout", TimeSpan.MinValue - tick));
            Assert.Throws<Exception>(() => c.Set("max_in_flight", 0 - 1));
            Assert.Throws<Exception>(() => c.Set("max_backoff_duration", TimeSpan.Zero - tick));
            Assert.Throws<Exception>(() => c.Set("msg_timeout", TimeSpan.Zero - tick));
            //Assert.Throws<Exception>(() => c.Set("auth_secret", null));
        }

        [Fact]
        public void TestValidatesGreaterThanMaxValues()
        {
            var c = new Config();
            var tick = new TimeSpan(1);

            Assert.Throws<Exception>(() => c.Set("read_timeout", TimeSpan.FromMinutes(5) + tick));
            Assert.Throws<Exception>(() => c.Set("write_timeout", TimeSpan.FromMinutes(5) + tick));
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_interval", TimeSpan.FromMinutes(5) + tick));
            Assert.Throws<Exception>(() => c.Set("lookupd_poll_jitter", 1 + 0.0001));
            Assert.Throws<Exception>(() => c.Set("max_requeue_delay", TimeSpan.FromMinutes(60) + tick));
            Assert.Throws<Exception>(() => c.Set("default_requeue_delay", TimeSpan.FromMinutes(60) + tick));
            Assert.Throws<Exception>(() => c.Set("backoff_multiplier", TimeSpan.FromMinutes(60) + tick));
            Assert.Throws<Exception>(() => c.Set("max_attempts", 65535 + 1));
            Assert.Throws<Exception>(() => c.Set("low_rdy_idle_timeout", TimeSpan.FromMinutes(5) + tick));
            //Assert.Throws<Exception>(() => c.Set("client_id", "my"));
            //Assert.Throws<Exception>(() => c.Set("hostname", "my.host.name.com"));
            //Assert.Throws<Exception>(() => c.Set("user_agent", "user-agent/1.0"));
            //Assert.Throws<Exception>(() => c.Set("heartbeat_interval", TimeSpan.MaxValue));
            Assert.Throws<Exception>(() => c.Set("sample_rate", 99 + 1));
            //Assert.Throws<Exception>(() => c.Set("tls_v1", true));
            //Assert.Throws<Exception>(() => c.Set("tls_config", tlsConfig);
            //Assert.Throws<Exception>(() => c.Set("deflate", true));
            Assert.Throws<Exception>(() => c.Set("deflate_level", 9 + 1));
            //Assert.Throws<Exception>(() => c.Set("snappy", true));
            //Assert.Throws<Exception>(() => c.Set("output_buffer_size", Int64.MaxValue));
            //Assert.Throws<Exception>(() => c.Set("output_buffer_timeout", TimeSpan.MaxValue));
            //Assert.Throws<Exception>(() => c.Set("max_in_flight", int.MaxValue));
            Assert.Throws<Exception>(() => c.Set("max_backoff_duration", TimeSpan.FromMinutes(60) + tick));
            //Assert.Throws<Exception>(() => c.Set("msg_timeout", TimeSpan.MaxValue));
            //Assert.Throws<Exception>(() => c.Set("auth_secret");
        }

        [Fact]
        public void TestHeartbeatLessThanReadTimout()
        {
            var c = new Config();

            c.Set("read_timeout", "5m");
            c.Set("heartbeat_interval", "2s");
            c.Validate();

            c.Set("read_timeout", "2s");
            c.Set("heartbeat_interval", "5m");
            Assert.Throws<Exception>(() => c.Validate());
        }

        [Fact]
        public void TestTls()
        {
            // TODO: Test more TLS

            var c = new Config();

            Assert.Equal(null, c.TlsConfig);

            c.Set("tls_insecure_skip_verify", true);
            Assert.True(c.TlsConfig.InsecureSkipVerify, "TlsConfig.InsecureSkipVerify");

            c.Set("tls_min_version", "ssl3.0");
            Assert.Equal(SslProtocols.Ssl3, c.TlsConfig.MinVersion);

            c.Set("tls_min_version", "tls1.0");
            Assert.Equal(SslProtocols.Tls, c.TlsConfig.MinVersion);

            c.Set("tls_min_version", "tls1.1");
            Assert.Equal(SslProtocols.Tls11, c.TlsConfig.MinVersion);

            c.Set("tls_min_version", "tls1.2");
            Assert.Equal(SslProtocols.Tls12, c.TlsConfig.MinVersion);

            Assert.Throws<Exception>(() => c.Set("tls_min_version", "ssl2.0"));
        }
    }
}
