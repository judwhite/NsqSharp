using System.Collections.ObjectModel;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Topic and channel information for the current process.
    /// </summary>
    public interface ITopicChannels
    {
        /// <summary>
        /// The topic name.
        /// </summary>
        string Topic { get; }
        /// <summary>
        /// The topic channels handled by this process.
        /// </summary>
        Collection<string> Channels { get; }
    }

    /// <summary>
    /// Topic and channel information for the current process.
    /// </summary>
    internal class TopicChannels : ITopicChannels
    {
        /// <summary>
        /// The topic name.
        /// </summary>
        public string Topic { get; set; }
        /// <summary>
        /// The topic channels handled by this process.
        /// </summary>
        public Collection<string> Channels { get; set; }
    }
}
