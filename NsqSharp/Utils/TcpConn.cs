using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using NsqSharp.Utils.Extensions;

namespace NsqSharp.Utils
{
    internal class TcpConn : ITcpConn
    {
        // TODO: Might be better to use Sockets than TcpClient http://angrez.blogspot.com/2007/02/flush-socket-in-net-or-c.html
        // https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.setsocketoption.aspx

        private readonly TcpClient _tcpClient;
        private readonly object _readLocker = new object();
        private readonly object _writeLocker = new object();
        private readonly object _closeLocker = new object();
        private readonly string _hostname;
        private Stream _networkStream;
        private bool _isClosed;

        public TcpConn(string hostname, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(hostname, port);
            _networkStream = _tcpClient.GetStream();

            _hostname = hostname;
        }

        public void UpgradeTls(TlsConfig tlsConfig)
        {
            if (tlsConfig == null)
                throw new ArgumentNullException("tlsConfig");

            lock (_readLocker)
            {
                lock (_writeLocker)
                {
                    const bool leaveInnerStreamOpen = false;

                    var enabledSslProtocols = tlsConfig.GetEnabledSslProtocols();

                    string errorMessage = null;

                    var sslStream = new SslStream(
                        _networkStream,
                        leaveInnerStreamOpen,
                        (sender, certificate, chain, sslPolicyErrors) =>
                            ValidateCertificates(chain, sslPolicyErrors, tlsConfig, out errorMessage)
                    );

                    try
                    {
                        sslStream.AuthenticateAsClient(_hostname, new X509Certificate2Collection(), enabledSslProtocols, tlsConfig.CheckCertificateRevocation);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("{0} - {1}", ex.Message, errorMessage), ex);
                    }

                    _networkStream = sslStream;
                }
            }
        }

        private static bool ValidateCertificates(X509Chain chain, SslPolicyErrors sslPolicyErrors, TlsConfig tlsConfig, out string errorMessage)
        {
            errorMessage = null;

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            {
                errorMessage = chain.ChainStatus.GetErrors();
                return false;
            }

            if (tlsConfig.InsecureSkipVerify || sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            else
            {
                errorMessage = chain.ChainStatus.GetErrors();
                return false;
            }
        }

        public TimeSpan ReadTimeout
        {
            get { return TimeSpan.FromMilliseconds(_tcpClient.ReceiveTimeout); }
            set { _tcpClient.ReceiveTimeout = (int)value.TotalMilliseconds; }
        }

        public TimeSpan WriteTimeout
        {
            get { return TimeSpan.FromMilliseconds(_tcpClient.SendTimeout); }
            set { _tcpClient.SendTimeout = (int)value.TotalMilliseconds; }
        }

        public int Read(byte[] b)
        {
            lock (_readLocker)
            {
                int byteLength = b.Length;

                int total = _networkStream.Read(b, 0, byteLength);
                if (total == byteLength || total == 0)
                    return total;

                while (total < byteLength)
                {
                    int n = _networkStream.Read(b, total, byteLength - total);
                    if (n == 0)
                        return total;
                    total += n;
                }
                return total;
            }
        }

        public int Write(byte[] b, int offset, int length)
        {
            lock (_writeLocker)
            {
                _networkStream.Write(b, offset, length);
                return length;
            }
        }

        public void Close()
        {
            if (_isClosed)
                return;

            lock (_writeLocker)
            {
                lock (_closeLocker)
                {
                    if (_isClosed)
                        return;

                    Flush();

                    _isClosed = true;

                    ReadTimeout = TimeSpan.FromMilliseconds(10);
                    WriteTimeout = TimeSpan.FromMilliseconds(10);

                    _networkStream.Close();
                    _tcpClient.Close();
                }
            }
        }

        public void CloseRead()
        {
            Close();
        }

        public void CloseWrite()
        {
            Close();
        }

        public void Flush()
        {
        }
    }
}
