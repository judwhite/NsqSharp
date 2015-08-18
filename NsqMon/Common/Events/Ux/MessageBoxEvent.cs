using System.Windows;
using NsqMon.Views;

namespace NsqMon.Common.Events.Ux
{
    /// <summary>
    /// MessageBoxEvent
    /// </summary>
    public class MessageBoxEvent
    {
        /// <summary>Gets or sets the owner.</summary>
        /// <value>The owner.</value>
        public IWindow Owner { get; set; }

        /// <summary>Gets or sets the message box text.</summary>
        /// <value>The message box text.</value>
        public string MessageBoxText { get; set; }

        /// <summary>Gets or sets the caption.</summary>
        /// <value>The caption.</value>
        public string Caption { get; set; }

        /// <summary>Gets or sets the message box button.</summary>
        /// <value>The message box button.</value>
        public MessageBoxButton MessageBoxButton { get; set; }

        /// <summary>Gets or sets the message box image.</summary>
        /// <value>The message box image.</value>
        public MessageBoxImage MessageBoxImage { get; set; }

        /// <summary>Gets or sets the result.</summary>
        /// <value>The result.</value>
        public MessageBoxResult Result { get; set; }
    }
}
