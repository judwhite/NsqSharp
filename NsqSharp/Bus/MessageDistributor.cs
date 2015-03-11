using System;
using System.Linq;
using System.Reflection;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Logging;
using NsqSharp.Bus.Utils;
using NsqSharp.Core;

namespace NsqSharp.Bus
{
    internal class MessageDistributor : IHandler
    {
        private readonly IObjectBuilder _objectBuilder;
        private readonly IMessageSerializer _serializer;
        private readonly MethodInfo _handleMethod;
        private readonly Type _handlerType;
        private readonly Type _messageType;
        private readonly Type _concreteMessageType;
        private readonly IFailedMessageHandler _failedMessageHandler;
        private readonly string _topic;
        private readonly string _channel;

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
            _failedMessageHandler = messageHandlerMetadata.FailedMessageHandler;
            _topic = messageHandlerMetadata.Topic;
            _channel = messageHandlerMetadata.Channel;

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

            if (!_messageType.IsInterface)
            {
                _concreteMessageType = _messageType;
            }
            else
            {
                _concreteMessageType = InterfaceBuilder.CreateType(_messageType);
            }
        }

        public void HandleMessage(Message message)
        {
            object handler;
            try
            {
                handler = _objectBuilder.GetInstance(_handlerType);
            }
            catch (Exception ex)
            {
                while (ex is TargetInvocationException && ex.InnerException != null)
                    ex = ex.InnerException;

                _failedMessageHandler.TryHandle(FailedMessageQueueAction.Finish, FailedMessageReason.HandlerConstructor,
                    _topic, _channel, _handlerType, _messageType, message, null, ex);
                message.Finish();
                throw;
            }

            object value;
            try
            {
                value = _serializer.Deserialize(_concreteMessageType, message.Body);
            }
            catch (Exception ex)
            {
                while (ex is TargetInvocationException && ex.InnerException != null)
                    ex = ex.InnerException;

                _failedMessageHandler.TryHandle(FailedMessageQueueAction.Finish, FailedMessageReason.MessageDeserialization,
                    _topic, _channel, _handlerType, _messageType, message, null, ex);
                message.Finish();
                return;
            }

            try
            {
                _handleMethod.Invoke(handler, new[] { value });
            }
            catch (Exception ex)
            {
                while (ex is TargetInvocationException && ex.InnerException != null)
                    ex = ex.InnerException;

                _failedMessageHandler.TryHandle(FailedMessageQueueAction.Requeue, FailedMessageReason.MessageDeserialization,
                    _topic, _channel, _handlerType, _messageType, message, value, ex);
                throw;
            }
        }

        public void LogFailedMessage(Message message)
        {
            object value = null;
            try
            {
                value = _serializer.Deserialize(_messageType, message.Body);
            }
            catch (Exception)
            {
            }

            _failedMessageHandler.TryHandle(FailedMessageQueueAction.Finish, FailedMessageReason.MaxAttemptsExceeded,
                _topic, _channel, _handlerType, _messageType, message, value, null);
        }
    }
}
