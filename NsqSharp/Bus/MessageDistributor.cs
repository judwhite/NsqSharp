using System;
using System.Linq;
using System.Reflection;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Logging;
using NsqSharp.Bus.Utils;
using NsqSharp.Core;
using NsqSharp.Logging;

namespace NsqSharp.Bus
{
    internal class MessageDistributor : IHandler
    {
        private readonly NsqBus _bus;
        private readonly IObjectBuilder _objectBuilder;
        private static readonly ILog _logger = LogProvider.For<MessageDistributor>();
        private readonly IMessageSerializer _serializer;
        private readonly MethodInfo _handleMethod;
        private readonly Type _handlerType;
        private readonly Type _messageType;
        private readonly Type _concreteMessageType;
        private readonly IMessageAuditor _messageAuditor;
        private readonly string _topic;
        private readonly string _channel;

        public MessageDistributor(
            NsqBus bus,
            IObjectBuilder objectBuilder,
            MessageHandlerMetadata messageHandlerMetadata
        )
        {
            if (bus == null)
                throw new ArgumentNullException("bus");
            if (objectBuilder == null)
                throw new ArgumentNullException("objectBuilder");

            if (messageHandlerMetadata == null)
                throw new ArgumentNullException("messageHandlerMetadata");

            _bus = bus;
            _objectBuilder = objectBuilder;

            _serializer = messageHandlerMetadata.Serializer;
            _handlerType = messageHandlerMetadata.HandlerType;
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
            try
            {
                handler = _objectBuilder.GetInstance(_handlerType);
                messageInformation.HandlerType = handler.GetType();
            }
            catch (Exception ex)
            {
                messageInformation.Finished = DateTime.UtcNow;

                _messageAuditor.TryOnFailed(_bus,
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

                _messageAuditor.TryOnFailed(_bus,
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
            _messageAuditor.TryOnReceived(_bus, messageInformation);

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

                _messageAuditor.TryOnFailed(_bus,
                    new FailedMessageInformation
                    (
                        messageInformation,
                        requeue ? FailedMessageQueueAction.Requeue : FailedMessageQueueAction.Finish,
                        requeue ? FailedMessageReason.HandlerException : FailedMessageReason.MaxAttemptsExceeded,
                        ex
                    )
                );

                return;
            }

            messageInformation.Finished = DateTime.UtcNow;

            _messageAuditor.TryOnSucceeded(_bus, messageInformation);
        }

        public void LogFailedMessage(IMessage message)
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

            _messageAuditor.TryOnFailed(_bus,
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
