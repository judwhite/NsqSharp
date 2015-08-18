using System;
using System.Windows;
using NsqMon.Common.Wpf;
using NsqMon.Views;

namespace NsqMon.Common.ApplicationServices
{
    /// <summary>
    /// IDialogService
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Shows the error on the main window.</summary>
        /// <param name="exception">The exception.</param>
        void ShowError(Exception exception);

        /// <summary>Shows the error.</summary>
        /// <param name="exception">The exception.</param>
        /// <param name="errorContainer">The error container.</param>
        void ShowError(Exception exception, IErrorContainer errorContainer);

        /// <summary>Shows a window.</summary>
        /// <param name="window">The window.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>The result of <see cref="IWindow.ShowDialog()"/>.</returns>
        bool? ShowWindow(IWindow window, Window owner);

        /// <summary>Shows a window.</summary>
        /// <typeparam name="T">The window type.</typeparam>
        /// <param name="owner">The owner.</param>
        /// <returns>The result of <see cref="IWindow.ShowDialog()"/>.</returns>
        bool? ShowWindow<T>(Window owner)
            where T : IWindow;

        /// <summary>Shows the open file dialog.</summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="filter">The file filter.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="fileName">Name of the file opened.</param>
        /// <returns><c>true</c> if a file is selected.</returns>
        bool? ShowOpenFileDialog(string title, string filter, Window owner, out string fileName);
    }
}
