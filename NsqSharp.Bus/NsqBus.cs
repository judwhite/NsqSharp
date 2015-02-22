using System;
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
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            Send(message, nsqdTcpAddresses);
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
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            Send(message, topic, nsqdTcpAddresses);
        }

        public void Defer<T>(TimeSpan delay, T message)
        {
            throw new NotImplementedException();
        }

        public void Defer<T>(DateTime processAt, T message)
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
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            SendLocal(message);
        }

        public T CreateInstance<T>()
        {
            return Configure.Instance.Builder.Build<T>();
        }
    }
}
