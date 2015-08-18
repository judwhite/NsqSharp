namespace NsqMon.Common.Events
{
    /// <summary>
    /// A helper class for providing generic EventArgs data with cancel notification.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="DataEventArgs{T}.Data"/> property.</typeparam>
    public sealed class CancelDataEventArgs<T> : DataEventArgs<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="data">The data item to send to event handlers.</param>
        public CancelDataEventArgs(T data)
            : base(data)
        {
            Cancel = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the action should cancel.
        /// </summary>
        /// <value><c>true</c> if the action should cancel; otherwise, <c>false</c>.</value>
        public bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets the cancel reason.
        /// </summary>
        /// <value>The cancel reason.</value>
        public string CancelReason { get; set; }
    }
}
