using System;
using System.ComponentModel;
using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Events;
using NsqMon.Common.Events.Ux;
using NsqMon.Common.Wpf;

namespace NsqMon.Common.Mvvm
{
    /// <summary>
    /// IViewModelBase
    /// </summary>
    public interface IViewModelBase : INotifyPropertyChanged
    {
        /// <summary>Occurs when MessageBox /> has been called.</summary>
        event EventHandler<DataEventArgs<MessageBoxEvent>> ShowMessageBox;

        /// <summary>Occurs when ShowWindow has been called.</summary>
        event EventHandler<DataEventArgs<ShowWindowEvent>> ShowDialogWindow;

        /// <summary>Occurs when ShowOpenFileDialog has been called.</summary>
        event EventHandler<DataEventArgs<ShowOpenFileDialogEvent>> ShowOpenFile;

        /// <summary>Gets or sets the error container.</summary>
        /// <value>The error container.</value>
        IErrorContainer ErrorContainer { get; set; }

        /// <summary>Gets or sets the close window action.</summary>
        /// <value>The close window action.</value>
        Action<bool?> CloseWindow { get; set; }

        /// <summary>Gets or sets the current visual state.</summary>
        /// <value>The current visual state.</value>
        string CurrentVisualState { get; set; }

        /// <summary>Gets the event aggregator.</summary>
        IEventAggregator EventAggregator { get; }
    }

    /// <summary>
    /// IViewModelBase
    /// </summary>
    /// <typeparam name="T">The view model type.</typeparam>
    public interface IViewModelBase<T> : IViewModelBase
    {
        /// <summary>Occurs when a property value changes.</summary>
        event EnhancedPropertyChangedEventHandler<T> EnhancedPropertyChanged;
    }
}
