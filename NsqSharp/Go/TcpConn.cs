using System.IO;
using System.Net.Sockets;

namespace NsqSharp.Go
{
    internal class TcpConn : IConn
    {
        // TODO: Might be better to use Sockets than TcpClient http://angrez.blogspot.com/2007/02/flush-socket-in-net-or-c.html
        // https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.setsocketoption.aspx

        private readonly TcpClient _tcpClient;
        private readonly Stream _stream;

        public TcpConn(string hostname, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(hostname, port);
            _stream = _tcpClient.GetStream();
        }

        public int Read(byte[] b)
        {
            return _stream.Read(b, 0, b.Length);
        }

        public int Write(byte[] b)
        {
            _stream.Write(b, 0, b.Length);
            return b.Length;
        }

        public void Close()
        {
            _tcpClient.Close();
        }
    }
}
