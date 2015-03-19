using System;
using System.Diagnostics;
using NsqSharp.Bus;
using PingPong.Messages;
using PingPong.Services;

namespace PingPong.Handlers
{
    // maps to channel ping-handler on topic pings
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
            // uncomment to see error handling, requeue, and backoff in action
            //if (DateTime.Now.Ticks % 50 == 0)
            //    throw new Exception("unlucky!");

            Console.WriteLine(string.Format("[{0:#,0}] {1}", _counter.Next(), ping.Message));
            _bus.Send(new PongMessage { Message = "Pong!" });
            //Thread.Sleep(250); // uncomment this line and try changing the value of defaultThreadsPerHandler in Program.cs
        }
    }
}
