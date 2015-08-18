using System.Windows;

namespace NsqMon.Views
{
    /// <summary>
    /// IWindow
    /// </summary>
    public interface IWindow
    {
        /// <summary>
        /// Gets or sets the window owner.
        /// </summary>
        /// <value>The window owner.</value>
        Window Owner { get; set; }

        /// <summary>
        /// Gets or sets the data context.
        /// </summary>
        /// <value>The data context.</value>
        object DataContext { get; set; }

        /// <summary>
        /// Closes the window.
        /// </summary>
        void Close();

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <returns><c>true</c> if OK clicked, <c>false</c> if Cancel clicked; otherwise, <c>null</c>.</returns>
        bool? ShowDialog();
    }
}
