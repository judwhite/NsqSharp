using System;
using System.Linq;
using System.Reflection;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus
{
    internal class MessageDistributor : IHandler
    {
        private readonly IObjectBuilder _objectBuilder;
        private readonly IMessageSerializer _serializer;
        private readonly MethodInfo _handleMethod;
        private readonly Type _handlerType;
        private readonly Type _messageType;

        public MessageDistributor(IObjectBuilder objectBuilder, MessageHandlerMetadata messageHandlerMetadata)
        {
            if (objectBuilder == null)
                throw new ArgumentNullException("objectBuilder");
            if (messageHandlerMetadata == null)
                throw new ArgumentNullException("messageHandlerMetadata");

            _objectBuilder = objectBuilder;
            _serializer = messageHandlerMetadata.Serializer;

            _handlerType = messageHandlerMetadata.HandlerType;
            _messageType = messageHandlerMetadata.MessageType;

            var possibleMethods = _handlerType.GetMethods().Where(p => p.Name == "Handle" && !p.IsGenericMethod);
            foreach (var possibleMethod in possibleMethods)
            {
                var parameters = possibleMethod.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == _messageType)
                {
                    _handleMethod = possibleMethod;
                    break;
                }
            }

            if (_handleMethod == null)
                throw new Exception(string.Format("Handle({0}) not found on {1}", _messageType, _handlerType));
        }

        public void HandleMessage(Message message)
        {
            object handler;
            try
            {
                handler = _objectBuilder.GetInstance(_handlerType);
            }
            catch (Exception)
            {
                // TODO: Log handler creation error
                message.Finish();
                throw;
            }

            object value;
            try
            {
                value = _serializer.Deserialize(_messageType, message.Body);
            }
            catch (Exception)
            {
                // TODO: Log serialization error
                message.Finish();
                return;
            }

            try
            {
                _handleMethod.Invoke(handler, new[] { value });
            }
            catch (Exception)
            {
                // TODO: Log
                throw;
            }
        }

        public void LogFailedMessage(Message message)
        {
            throw new NotImplementedException();
        }
    }
}
