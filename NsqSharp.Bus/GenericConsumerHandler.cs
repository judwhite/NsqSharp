using System;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus
{
    internal class GenericConsumerHandler : IHandler
    {
        public GenericConsumerHandler(MessageHandlerMetadata messageHandlerMetadata)
        {
            if (messageHandlerMetadata == null)
                throw new ArgumentNullException("messageHandlerMetadata");

            throw new NotImplementedException();
        }

        public void HandleMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public void LogFailedMessage(Message message)
        {
            throw new NotImplementedException();
        }
    }
}
