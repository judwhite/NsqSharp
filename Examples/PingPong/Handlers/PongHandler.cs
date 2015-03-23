using System;
using NsqSharp.Bus;
using PingPong.Messages;
using PingPong.Services;

namespace PingPong.Handlers
{
    // Maps to Channel "pong-handler" on Topic "pongs"
    public class PongHandler : IHandleMessages<PongMessage>
    {
        private readonly IBus _bus;
        private readonly ICounter _counter;

        public PongHandler(IBus bus, ICounter counter)
        {
            _bus = bus;
            _counter = counter;
        }

        public void Handle(PongMessage pong)
        {
            Console.WriteLine(string.Format("[{0:#,0}] {1}", _counter.Next(), pong.Message));
            _bus.Send<PingMessage>(p => { p.Message = "Ping!"; });
            //Thread.Sleep(250); // uncomment this line and try changing the value of defaultThreadsPerHandler in Program.cs
        }
    }
}
