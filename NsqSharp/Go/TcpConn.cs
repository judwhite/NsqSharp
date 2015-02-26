using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace NsqSharp.Go
{
    internal class TcpConn : ITcpConn
    {
        // TODO: Might be better to use Sockets than TcpClient http://angrez.blogspot.com/2007/02/flush-socket-in-net-or-c.html
        // https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.setsocketoption.aspx

        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _networkStream;
        private readonly object _readLocker = new object();
        private readonly object _writeLocker = new object();
        private readonly object _closeLocker = new object();
        private bool _isClosed;

        public TcpConn(string hostname, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(hostname, port);
            _networkStream = _tcpClient.GetStream();
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
                return _networkStream.Read(b, 0, b.Length);
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

                    Time.AfterFunc(TimeSpan.FromSeconds(2), () =>
                    {
                        _networkStream.Close();
                        _tcpClient.Close();
                    });
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
