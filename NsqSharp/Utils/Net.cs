using System;
using NsqSharp.Utils.Channels;

namespace NsqSharp.Utils
{
    /// <summary>
    /// Net package. http://golang.org/pkg/net
    /// </summary>
    public static class Net
    {
        /// <summary>
        /// Dial connects to the address on the named network.
        /// 
        /// Known networks are "tcp" only at this time.
        /// 
        /// Addresses have the form host:port. If host is a literal IPv6 address it must be enclosed in square brackets as in
        /// "[::1]:80" or "[ipv6-host%zone]:80". The functions JoinHostPort and SplitHostPort manipulate addresses in this form.
        /// </summary>
        public static IConn Dial(string network, string address)
        {
            if (network != "tcp")
                throw new ArgumentException("only 'tcp' network is supported", "network");

            // TODO: Make this more robust, support IPv6 splitting
            var split = address.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            string hostname = split[0];
            int port = int.Parse(split[1]);

            return new TcpConn(hostname, port);
        }

        /// <summary>
        /// DialTimeout acts like Dial but takes a timeout. The timeout includes name resolution, if required.
        /// </summary>
        public static IConn DialTimeout(string network, string address, TimeSpan timeout)
        {
            if (network != "tcp")
                throw new ArgumentException("only 'tcp' network is supported", "network");

            var dialChan = new Chan<IConn>();
            var timeoutChan = Time.After(timeout);

            GoFunc.Run(() =>
            {
                try
                {
                    var tmpConn = Dial(network, address);
                    dialChan.Send(tmpConn);
                }
                catch
                {
                    // handling timeout below, don't bring down the whole app with an unhandled thread exception
                }
            }, "Net:DialTimeout");

            IConn conn = null;

            Select
                .CaseReceive(dialChan, c => conn = c)
                .CaseReceive(timeoutChan, o =>
                {
                    throw new TimeoutException(string.Format("timeout {0} exceed when dialing {1}", timeout, address));
                })
                .NoDefault();

            timeoutChan.Close();

            return conn;
        }
    }
}
