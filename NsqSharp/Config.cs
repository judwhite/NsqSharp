using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NsqSharp.Attributes;

namespace NsqSharp
{
    /// <summary>
    /// Define handlers for setting config defaults, and setting config values from command line arguments or config files
    /// </summary>
    internal interface configHandler
    {
        bool HandlesOption(Config c, string option);
        void Set(Config c, string option, object value);
        void Validate(Config c);
    }

    internal interface defaultsHandler
    {
        void SetDefaults(Config c);
    }

    /// <summary>
    /// Config is a struct of NSQ options
    ///
    /// The only valid way to create a Config is via NewConfig, using a struct literal will panic.
    /// After Config is passed into a high-level type (like Consumer, Producer, etc.) the values are no
    /// longer mutable (they are copied).
    ///
    /// Use Set(key string, value interface{}) as an alternate way to set parameters
    /// </summary>
    public class Config
    {
        //private bool initialized;

        // used to Initialize, Validate
        //private List<configHandler> configHandlers;

        /// <summary>Deadline for network reads</summary>
        [Opt("read_timeout"), Min("100ms"), Max("5m"), Default("60s", isDuration: true)]
        public TimeSpan ReadTimeout { get; internal set; }

        /// <summary>Deadline for network writes</summary>
        [Opt("read_timeout"), Min("100ms"), Max("5m"), Default("1s", isDuration: true)]
        public TimeSpan WriteTimeout { get; internal set; }
    }
}
