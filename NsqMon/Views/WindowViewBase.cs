using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NsqMon.Common;
using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Events;
using NsqMon.Common.Events.Ux;
using NsqMon.Common.Mvvm;

namespace NsqMon.Views
{
    namespace CDTag.Views
    {
        /// <summary>
        /// WindowViewBase. Handles generic window settings.
        /// </summary>
        public class WindowViewBase : Window, IWindow
        {
            public static readonly DependencyProperty CurrentVisualStateProperty =
                DependencyProperty.Register("CurrentVisualState", typeof(string), typeof(WindowViewBase), new PropertyMetadata(default(string), CurrentVisualStateChanged));

            public static readonly DependencyProperty HandleEscapeProperty =
                DependencyProperty.Register("HandleEscape", typeof(bool), typeof(WindowViewBase), new PropertyMetadata(default(bool)));

            private readonly IViewModelBase _viewModel;
            private bool _settingsLoaded;

            private static readonly IDialogService _dialogService;

            static WindowViewBase()
            {
                _dialogService = IoC.Resolve<IDialogService>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="WindowViewBase"/> class.
            /// </summary>
            /// <param name="viewModel">The view model.</param>
            protected WindowViewBase(IViewModelBase viewModel)
            {
                if (viewModel == null)
                    throw new ArgumentNullException("viewModel");

                _viewModel = viewModel;
                DataContext = _viewModel;

                PreviewKeyDown += WindowViewBase_PreviewKeyDown;
                Closed += WindowViewBase_Closed;
                HandleEscape = true;
                ShowInTaskbar = false;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                FontFamily = new FontFamily("Verdana");
                FontSize = 11.0d;
                Background = (Brush)Application.Current.Resources["WindowBackground"];

                _viewModel.ShowMessageBox += viewModel_ShowMessageBox;
                _viewModel.ShowDialogWindow += viewModel_ShowDialogWindow;
                _viewModel.ShowOpenFile += viewModel_ShowOpenFile;
            }

            private void viewModel_ShowOpenFile(object sender, DataEventArgs<ShowOpenFileDialogEvent> e)
            {
                if (e == null)
                    throw new ArgumentNullException("e");

                var ofd = e.Data;
                string fileName;
                bool? result = _dialogService.ShowOpenFileDialog(ofd.Title, ofd.Filter, this, out fileName);
                ofd.FileName = fileName;
                ofd.Result = result;
            }

            private void viewModel_ShowDialogWindow(object sender, DataEventArgs<ShowWindowEvent> e)
            {
                if (e == null)
                    throw new ArgumentNullException("e");

                bool? result = _dialogService.ShowWindow(e.Data.IWindow, this);
                e.Data.Result = result;
            }

            private void viewModel_ShowMessageBox(object sender, DataEventArgs<MessageBoxEvent> e)
            {
                if (e == null)
                    throw new ArgumentNullException("e");

                MessageBoxEvent messageBoxEvent = e.Data;
                messageBoxEvent.Owner = this;

                _viewModel.EventAggregator.Publish(messageBoxEvent);
            }

            /// <summary>
            /// Raises the <see cref="E:System.Windows.FrameworkElement.Initialized"/> event. This method is invoked whenever <see cref="P:System.Windows.FrameworkElement.IsInitialized"/> is set to true internally.
            /// </summary>
            /// <param name="e">The <see cref="T:System.Windows.RoutedEventArgs"/> that contains the event data.</param>
            protected override void OnInitialized(EventArgs e)
            {
                base.OnInitialized(e);

                if (!_settingsLoaded)
                    LoadWindowSettings();
            }

            /// <summary>Property changed handler for <see cref="CurrentVisualState" /> dependency property.</summary>
            /// <param name="dependencyObject">The dependency object.</param>
            /// <param name="dependencyPropertyChangedEventArgs">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
            private static void CurrentVisualStateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
            {
                var window = (WindowViewBase)dependencyObject;
                if (window.CurrentVisualState != null)
                {
                    VisualStateManager.GoToElementState(window, window.CurrentVisualState, useTransitions: true);
                }
            }

            /// <summary>Gets or sets the current visual state.</summary>
            /// <value>The current visual state.</value>
            public string CurrentVisualState
            {
                get { return (string)GetValue(CurrentVisualStateProperty); }
                set { SetValue(CurrentVisualStateProperty, value); }
            }

            /// <summary>
            /// Gets or sets a value indicating whether the escape key should be used to close the window.
            /// </summary>
            /// <value><c>true</c> if the escape key should be used to close the window; otherwise, <c>false</c>.</value>
            public bool HandleEscape
            {
                get { return (bool)GetValue(HandleEscapeProperty); }
                set { SetValue(HandleEscapeProperty, value); }
            }

            private void LoadWindowSettings()
            {
                _settingsLoaded = true;

                /*const string fileName = "windows.json";
                Dictionary<string, WindowSettings> windows;
                if (SettingsFile.TryLoad(fileName, out windows))
                {
                    WindowSettings windowSettings;
                    if (windows.TryGetValue(Name, out windowSettings))
                    {
                        Height = windowSettings.Height ?? Height;
                        Width = windowSettings.Width ?? Width;
                        if (WindowStartupLocation != WindowStartupLocation.CenterOwner)
                        {
                            Top = windowSettings.Top ?? Top;
                            Left = windowSettings.Left ?? Left;
                        }
                        WindowState = (windowSettings.WindowState == null || windowSettings.WindowState == WindowState.Minimized) ? WindowState : windowSettings.WindowState.Value;
                    }
                }*/
            }

            private void WindowViewBase_Closed(object sender, EventArgs e)
            {
                /*if (string.IsNullOrWhiteSpace(Name))
                    return;

                const string fileName = "windows.json";
                Dictionary<string, WindowSettings> windows;
                if (!SettingsFile.TryLoad(fileName, out windows))
                {
                    windows = new Dictionary<string, WindowSettings>();
                }

                WindowSettings windowSettings;
                if (windows.TryGetValue(Name, out windowSettings))
                {
                    // Preserve old values if WindowState != Normal
                    if (WindowState == WindowState.Normal)
                    {
                        windowSettings.Height = Height;
                        windowSettings.Width = Width;
                        windowSettings.Top = Top;
                        windowSettings.Left = Left;
                    }
                    windowSettings.WindowState = WindowState;
                }
                else
                {
                    windowSettings = new WindowSettings();
                    windowSettings.Height = (WindowState == WindowState.Normal ? Height : (double?)null);
                    windowSettings.Width = (WindowState == WindowState.Normal ? Width : (double?)null);
                    windowSettings.Top = (WindowState == WindowState.Normal ? Top : (double?)null);
                    windowSettings.Left = (WindowState == WindowState.Normal ? Left : (double?)null);
                    windowSettings.WindowState = WindowState;

                    windows.Add(Name, windowSettings);
                }

                SettingsFile.Save(fileName, windows);*/
            }

            private void WindowViewBase_PreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (HandleEscape)
                {
                    if (e.Key == Key.Escape)
                    {
                        Close();
                        e.Handled = true;
                    }
                }
            }

            /// <summary>
            /// Only here to support XAML. Use <see cref="M:WindowViewBase(IViewModelBase)" /> instead.
            /// </summary>
            public WindowViewBase()
            {
                // Note: only here to support XAML
                throw new NotSupportedException();
            }
        }
    }
}
