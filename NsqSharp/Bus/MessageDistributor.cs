﻿using System;
using System.Linq;
using System.Reflection;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Logging;
using NsqSharp.Core;

namespace NsqSharp.Bus
{
    internal class MessageDistributor : IHandler
    {
        private readonly NsqBus _bus;
        private readonly Core.ILogger _logger;
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
            Core.ILogger logger,
            MessageHandlerMetadata messageHandlerMetadata
        )
        {
            if (bus == null)
                throw new ArgumentNullException("bus");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (messageHandlerMetadata == null)
                throw new ArgumentNullException("messageHandlerMetadata");

            _bus = bus;
            _logger = logger;
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
                //_concreteMessageType = InterfaceBuilder.CreateType(_messageType);
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

            // TODO Object Builder stuff
            // Get handler
            // object handler;
            // try
            // {
            //     handler = _objectBuilder.GetInstance(_handlerType);
            //     messageInformation.HandlerType = handler.GetType();
            // }
            // catch (Exception ex)
            // {
            //     messageInformation.Finished = DateTime.UtcNow;

            //     _messageAuditor.TryOnFailed(_logger, _bus,
            //         new FailedMessageInformation
            //         (
            //             messageInformation,
            //             FailedMessageQueueAction.Finish,
            //             FailedMessageReason.HandlerConstructor,
            //             ex
            //         )
            //     );

            //     message.Finish();
            //     return;
            // }

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

            // TODO Object Builder stuff
            // try
            // {
            //     _handleMethod.Invoke(handler, new[] { value });
            // }
            // catch (Exception ex)
            // {
            //     messageInformation.Finished = DateTime.UtcNow;

            //     if (!message.HasResponded)
            //     {
            //         if (message.Attempts < message.MaxAttempts)
            //             message.Requeue();
            //         else
            //             message.Finish();
            //     }

            //     bool requeued = (message.RequeuedUntil != null);
            //     bool maxAttemptsExceeded = (message.Attempts >= message.MaxAttempts);

            //     _messageAuditor.TryOnFailed(_logger, _bus,
            //         new FailedMessageInformation
            //         (
            //             messageInformation,
            //             requeued ? FailedMessageQueueAction.Requeue : FailedMessageQueueAction.Finish,
            //             maxAttemptsExceeded ? FailedMessageReason.MaxAttemptsExceeded : FailedMessageReason.HandlerException,
            //             ex
            //         )
            //     );

            //     return;
            // }

            messageInformation.Finished = DateTime.UtcNow;

            _messageAuditor.TryOnSucceeded(_logger, _bus, messageInformation);
        }

        public void LogFailedMessage(IMessage message)
        {
            // TODO Object Builder stuff
            // object handler;
            // try
            // {
            //     handler = _objectBuilder.GetInstance(_handlerType);
            // }
            // catch
            // {
            //     handler = _handlerType;
            // }

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
                //HandlerType = handler.GetType(),
                // TODO Object Builder stuff
                HandlerType = message.GetType(),
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
