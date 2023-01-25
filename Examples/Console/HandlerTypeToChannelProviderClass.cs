using System.Diagnostics;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration.Providers;

internal class HandlerTypeToChannelProviderClass : IHandlerTypeToChannelProvider
{
        // every handler maps to a channel off a topic.
        // channels are independent listeners to the stream of messages sent to a topic.

        // a handler is an implementation of IHandleMessages<T>.

        private readonly Dictionary<Type, string> _handlerToChannel = new Dictionary<Type, string>();

        public HandlerTypeToChannelProviderClass()
        {
            _handlerToChannel.Add(typeof(PingHandler), "ping-handler");
            _handlerToChannel.Add(typeof(PongHandler), "pong-handler");
        }

        public string GetChannel(Type handlerType)
        {
            return _handlerToChannel[handlerType];
        }

        public IEnumerable<Type> GetHandlerTypes()
        {
            return _handlerToChannel.Keys;
        }


    public interface ICounter
    {
        int Next();
        int Current { get; }
        TimeSpan Elapsed { get; }
    }

        // used in this demo for counting handled messages
    public class Counter : ICounter
    {
        private readonly Stopwatch _stopwatch;
        private int _count;

        public Counter()
        {
            _stopwatch = new Stopwatch();
        }

        public int Next()
        {
            int num = Interlocked.Increment(ref _count);
            if (num == 1)
                _stopwatch.Start();
            return num;
        }

        public int Current { get { return _count; } }
        public TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }
    }

        // maps to topic "pings"
    public class PingMessage
    {
        public string Message { get; set; }
    }

            // maps to topic "pings"
    public class PongMessage
    {
        public string Message { get; set; }
    }
        
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
        public class PongHandler : IHandleMessages<PingMessage>
    {
        private readonly IBus _bus;
        private readonly ICounter _counter;

        public PongHandler(IBus bus, ICounter counter)
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