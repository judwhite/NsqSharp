using System;
using System.Collections.Generic;
using NsqSharp.Bus.Utils;

namespace NsqSharp.Bus.Configuration.Converters
{
    internal interface IHandlerTypeToChannelConverter
    {
        string GetChannel(Type handlerType);
    }

    internal class HandlerTypeToChannelConverter : IHandlerTypeToChannelConverter
    {
        private readonly Dictionary<Type, string> _handlerChannels = new Dictionary<Type, string>();
        private readonly object _handlerChannelsLocker = new object();

        public string GetChannel(Type handlerType)
        {
            if (handlerType == null)
                throw new ArgumentNullException("handlerType");

            lock (_handlerChannelsLocker)
            {
                string channelName;
                if (_handlerChannels.TryGetValue(handlerType, out channelName))
                    return channelName;

                channelName = string.Format("{0}.{1}", handlerType.Namespace, handlerType.Name);
                if (channelName.Length > 64)
                {
                    string crc32 = Crc32.Calculate(channelName);
                    string shortName = channelName.Substring(channelName.Length - 64 + 9);
                    int dotIdx = shortName.IndexOf('.');
                    if (dotIdx != -1)
                        shortName = shortName.Substring(dotIdx + 1);
                    channelName = string.Format("{0}-{1}", shortName, crc32);
                }

                channelName = channelName.Replace('.', '-');

                _handlerChannels.Add(handlerType, channelName);

                return channelName;
            }
        }
    }
}
