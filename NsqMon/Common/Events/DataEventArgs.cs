using System;

namespace NsqMon.Common.Events
{
    /// <summary>
    /// A helper class for providing generic EventArgs data.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="DataEventArgs{T}.Data"/> property.</typeparam>
    public class DataEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="data">The data item to send to event handlers.</param>
        public DataEventArgs(T data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets the data value.
        /// </summary>
        /// <value>The data value.</value>
        public T Data
        {
            get;
            set;
        }
    }
}
