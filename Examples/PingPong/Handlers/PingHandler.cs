using System;
using NsqSharp.Bus;
using PingPong.Messages;
using PingPong.Services;

namespace PingPong.Handlers
{
    // Maps to Channel "ping-handler" on Topic "pings"
    public class PingHandler : IHandleMessages<PingMessage>
    {
        private readonly IBus _bus;
        private readonly ICounter _counter;

        public PingHandler(IBus bus, ICounter counter)
        {
            _bus = bus;
            _counter = counter;
        }

        public void Handle(PingMessage ping)
        {
            Console.WriteLine(string.Format("[{0:#,0}] {1}", _counter.Next(), ping.Message));
            _bus.Send(new PongMessage { Message = "Pong!" });
            //Thread.Sleep(250); // uncomment this line and try changing the value of defaultThreadsPerHandler in Program.cs
        }
    }
}
