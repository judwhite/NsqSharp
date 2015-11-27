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
        private readonly NsqBus _bus;
        private readonly IObjectBuilder _objectBuilder;
        private readonly ILogger _logger;
        private readonly IMessageSerializer _serializer;
        private readonly MethodInfo _handleMethod;
        private readonly Type _handlerType;
        private readonly MethodInfo _messageSucceededMethod;
        private readonly MethodInfo _messageFailedMethod;
        private readonly Type _finalizerType;
        private readonly Type _messageType;
        private readonly Type _concreteMessageType;
        private readonly IMessageAuditor _messageAuditor;
        private readonly string _topic;
        private readonly string _channel;

        public MessageDistributor(
            NsqBus bus,
            IObjectBuilder objectBuilder,
            ILogger logger,
            MessageHandlerMetadata messageHandlerMetadata
        )
        {
            if (bus == null)
                throw new ArgumentNullException("bus");
            if (objectBuilder == null)
                throw new ArgumentNullException("objectBuilder");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (messageHandlerMetadata == null)
                throw new ArgumentNullException("messageHandlerMetadata");

            _bus = bus;
            _objectBuilder = objectBuilder;
            _logger = logger;

            _serializer = messageHandlerMetadata.Serializer;
            _handlerType = messageHandlerMetadata.HandlerType;
            _finalizerType = messageHandlerMetadata.FinalizerType;
            _messageType = messageHandlerMetadata.MessageType;
            _messageAuditor = messageHandlerMetadata.MessageAuditor;
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

            if (_finalizerType != null)
            {
                var possibleSucceededMethods = _finalizerType.GetMethods().Where(p => p.Name == "MessageSucceeded" && !p.IsGenericMethod);
                var possibleFailedMethods = _finalizerType.GetMethods().Where(p => p.Name == "MessageFailed" && !p.IsGenericMethod);

                foreach (var succeededMethod in possibleSucceededMethods)
                {
                    var parameters = succeededMethod.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == _messageType)
                    {
                        _messageSucceededMethod = succeededMethod;
                        break;
                    }
                }

                foreach (var failedMethod in possibleFailedMethods)
                {
                    var parameters = failedMethod.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == _messageType)
                    {
                        _messageFailedMethod = failedMethod;
                        break;
                    }
                }
            }
            else
            {
                _messageSucceededMethod = null;
                _messageFailedMethod = null;
            }
        }

        public void HandleMessage(IMessage message)
        {
            var messageInformation = new MessageInformation
                                     {
                                         UniqueIdentifier = Guid.NewGuid(),
                                         Topic = _topic,
                                         Channel = _channel,
                                         HandlerType = _handlerType,
                                         MessageType = _messageType,
                                         Message = message,
                                         DeserializedMessageBody = null,
                                         Started = DateTime.UtcNow
                                     };

            _bus.SetCurrentMessageInformation(messageInformation);

            // Get handler
            object handler;
            object finalzier = null;
            try
            {
                handler = _objectBuilder.GetInstance(_handlerType);
                messageInformation.HandlerType = handler.GetType();

                if (_finalizerType != null)
                {
                    finalzier = _objectBuilder.GetInstance(_finalizerType);
                    messageInformation.FinalizerType = finalzier.GetType();
                }
            }
            catch (Exception ex)
            {
                messageInformation.Finished = DateTime.UtcNow;

                _messageAuditor.TryOnFailed(_logger, _bus,
                    new FailedMessageInformation
                    (
                        messageInformation,
                        FailedMessageQueueAction.Finish,
                        FailedMessageReason.HandlerConstructor,
                        ex
                    )
                );

                message.Finish();
                return;
            }

            // Get deserialized value
            object value;
            try
            {
                value = _serializer.Deserialize(_concreteMessageType, message.Body);
            }
            catch (Exception ex)
            {
                messageInformation.Finished = DateTime.UtcNow;

                _messageAuditor.TryOnFailed(_logger, _bus,
                    new FailedMessageInformation
                    (
                        messageInformation,
                        FailedMessageQueueAction.Finish,
                        FailedMessageReason.MessageDeserialization,
                        ex
                    )
                );

                message.Finish();
                return;
            }

            // Handle message
            messageInformation.DeserializedMessageBody = value;
            _messageAuditor.TryOnReceived(_logger, _bus, messageInformation);

            try
            {
                _handleMethod.Invoke(handler, new[] { value });
            }
            catch (Exception ex)
            {
                bool requeue = (message.Attempts < message.MaxAttempts);

                messageInformation.Finished = DateTime.UtcNow;

                if (requeue)
                    message.Requeue();
                else
                    message.Finish();

                _messageAuditor.TryOnFailed(_logger, _bus,
                    new FailedMessageInformation
                    (
                        messageInformation,
                        requeue ? FailedMessageQueueAction.Requeue : FailedMessageQueueAction.Finish,
                        requeue ? FailedMessageReason.HandlerException : FailedMessageReason.MaxAttemptsExceeded,
                        ex
                    )
                );

                if (!requeue && finalzier != null && _messageFailedMethod != null)
                {
                    try
                    {
                        _messageFailedMethod.Invoke(finalzier, new[] { value });
                    }
                    catch
                    {

                    }
                }

                return;
            }

            messageInformation.Finished = DateTime.UtcNow;

            if (finalzier!=null && _messageSucceededMethod != null)
            {
                try
                {
                    _messageSucceededMethod.Invoke(finalzier, new[] {value});
                }
                catch
                {
                    
                }
            }
            _messageAuditor.TryOnSucceeded(_logger, _bus, messageInformation);
        }

        public void LogFailedMessage(IMessage message)
        {
            object handler;
            try
            {
                handler = _objectBuilder.GetInstance(_handlerType);
            }
            catch
            {
                handler = _handlerType;
            }

            object deserializedMessageBody;
            try
            {
                deserializedMessageBody = _serializer.Deserialize(_concreteMessageType, message.Body);
            }
            catch
            {
                deserializedMessageBody = null;
            }

            var messageInformation = new MessageInformation
            {
                UniqueIdentifier = Guid.NewGuid(),
                Topic = _topic,
                Channel = _channel,
                HandlerType = handler.GetType(),
                MessageType = _messageType,
                Message = message,
                DeserializedMessageBody = deserializedMessageBody,
                Started = DateTime.UtcNow
            };

            _messageAuditor.TryOnFailed(_logger, _bus,
                new FailedMessageInformation
                (
                    messageInformation,
                    FailedMessageQueueAction.Finish,
                    FailedMessageReason.MaxAttemptsExceeded,
                    null
                )
            );
        }
    }
}
