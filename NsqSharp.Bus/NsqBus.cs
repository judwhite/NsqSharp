using System;
using System.Runtime.Remoting.Messaging;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus
{
    internal class NsqBus : IBus
    {
        public void Send<T>(T message)
        {
            throw new NotImplementedException();
        }

        public void Send<T>()
        {
            throw new NotImplementedException();
        }

        public void Send<T>(Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public void Send<T>(T message, params string[] nsqdTcpAddresses)
        {
            throw new NotImplementedException();
        }

        public void Send<T>(params string[] nsqdTcpAddresses)
        {
            throw new NotImplementedException();
        }

        public void Send<T>(Action<T> messageConstructor, params string[] nsqdTcpAddresses)
        {
            throw new NotImplementedException();
        }

        public void Send<T>(T message, string topic, params string[] nsqdTcpAddresses)
        {
            throw new NotImplementedException();
        }

        public void Send<T>(string topic, params string[] nsqdTcpAddresses)
        {
            throw new NotImplementedException();
        }

        public void Send<T>(Action<T> messageConstructor, string topic, params string[] nsqdTcpAddresses)
        {
            throw new NotImplementedException();
        }

        public void Defer(TimeSpan delay)
        {
            throw new NotImplementedException();
        }

        public void Defer(DateTime processAt)
        {
            throw new NotImplementedException();
        }

        public IMessage CurrentMessage
        {
            get { throw new NotImplementedException(); }
        }

        public void SendLocal<T>(T message)
        {
            throw new NotImplementedException();
        }

        public void SendLocal<T>()
        {
            throw new NotImplementedException();
        }

        public void SendLocal<T>(Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public T CreateInstance<T>()
        {
            return Configure.Instance.Builder.Build<T>();
        }

        public void Publish<T>(T message)
        {
            Send(message);
        }

        public void Publish<T>()
        {
            Send<T>();
        }

        public void Publish<T>(Action<T> messageConstructor)
        {
            Send(messageConstructor);
        }

        public void Publish<T>(T message, params string[] nsqdTcpAddresses)
        {
            Send(message, nsqdTcpAddresses);
        }

        public void Publish<T>(params string[] nsqdTcpAddresses)
        {
            Send<T>(nsqdTcpAddresses);
        }

        public void Publish<T>(Action<T> messageConstructor, params string[] nsqdTcpAddresses)
        {
            Send(messageConstructor, nsqdTcpAddresses);
        }

        public void Publish<T>(T message, string topic, params string[] nsqdTcpAddresses)
        {
            Send(message, topic, nsqdTcpAddresses);
        }

        public void Publish<T>(string topic, params string[] nsqdTcpAddresses)
        {
            Send<T>(topic, nsqdTcpAddresses);
        }

        public void Publish<T>(Action<T> messageConstructor, string topic, params string[] nsqdTcpAddresses)
        {
            Send(messageConstructor, topic, nsqdTcpAddresses);
        }
    }
}
