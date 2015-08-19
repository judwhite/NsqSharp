using System.ComponentModel;

namespace NsqMon.Common.Events.Ux
{
    /// <summary>EnhancedPropertyChangedEventArgs</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EnhancedPropertyChangedEventArgs"/> instance containing the event data.</param>
    public delegate void EnhancedPropertyChangedEventHandler(object sender, EnhancedPropertyChangedEventArgs e);

    /// <summary>EnhancedPropertyChangedEventArgs</summary>
    public class EnhancedPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public EnhancedPropertyChangedEventArgs(string propertyName, object oldValue, object newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>Gets the old value.</summary>
        public object OldValue { get; private set; }

        /// <summary>Gets the new value.</summary>
        public object NewValue { get; private set; }
    }
}
