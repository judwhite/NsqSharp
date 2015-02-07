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
        private readonly NetworkStream _stream;
        private readonly object _readLocker = new object();
        private readonly object _writeLocker = new object();

        public TcpConn(string hostname, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(hostname, port);
            _stream = _tcpClient.GetStream();
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
                return _stream.Read(b, 0, b.Length);
            }
        }

        public int Write(byte[] b)
        {
            lock (_writeLocker)
            {
                _stream.Write(b, 0, b.Length);
                Debug.WriteLine("[NET] wrote {0:#,0} bytes", b.Length);
                return b.Length;
            }
        }

        public void Close()
        {
            _stream.Close();
            _tcpClient.Close();
        }

        public void CloseRead()
        {
            // TODO - No separate read/write streams
            Close();
        }

        public void CloseWrite()
        {
            // TODO - No separate read/write streams
            Close();
        }
    }
}
