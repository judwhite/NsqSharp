using System;
using System.Linq;
using System.Security.Authentication;

namespace NsqSharp.Utils
{
    /// <summary>
    /// TlsConfig. Minimal implementation of http://golang.org/pkg/crypto/tls/#Config
    /// </summary>
    public class TlsConfig
    {
        /// <summary>
        /// Initializes a new instance of the TlsConfig class.
        /// </summary>
        public TlsConfig()
        {
#if NETFX_3_5 || NETFX_4_0
            MinVersion = SslProtocols.Tls;
#else
            MinVersion = SslProtocols.Tls12;
#endif
            CheckCertificateRevocation = true;
        }

        /// <summary>
        /// Minimum TLS version (default = TLS 1.2 for .NET 4.5 and higher, TLS 1.0 for .NET 4.0 and lower).
        /// </summary>
        public SslProtocols MinVersion { get; set; }

        /// <summary>
        /// InsecureSkipVerify controls whether a client verifies the
        /// server's certificate chain and host name.
        /// If InsecureSkipVerify is true, TLS accepts any certificate
        /// presented by the server and any host name in that certificate.
        /// In this mode, TLS is susceptible to man-in-the-middle attacks.
        /// This should be used only for testing.
        /// 
        /// Overrides <see cref="CheckCertificateRevocation"/>.
        /// </summary>
        public bool InsecureSkipVerify { get; set; }

        /// <summary>Gets or sets a value indicating whether to check certificate revocation (default = true).</summary>
        /// <value>true if certificate revocation should be checked, false if not.</value>
        public bool CheckCertificateRevocation { get; set; }

        /// <summary>
        /// Gets the enabled <see cref="SslProtocols"/> based on <see cref="MinVersion"/>.
        /// </summary>
        /// <returns>The enabled <see cref="SslProtocols"/>.</returns>
        public SslProtocols GetEnabledSslProtocols()
        {
            int intSslProtocols = 0;
            int minSslProtocol = (int)MinVersion;

            foreach (var sslProtocol in Enum.GetValues(typeof(SslProtocols)).Cast<int>())
            {
                if (sslProtocol >= minSslProtocol && sslProtocol != (int)SslProtocols.Default)
                    intSslProtocols |= sslProtocol;
            }

            return (SslProtocols)intSslProtocols;
        }

        internal TlsConfig Clone()
        {
            return new TlsConfig
            {
                MinVersion = MinVersion,
                InsecureSkipVerify = InsecureSkipVerify,
                CheckCertificateRevocation = CheckCertificateRevocation
            };
        }
    }
}
