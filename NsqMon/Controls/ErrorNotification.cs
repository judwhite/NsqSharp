using System;
using System.Windows;
using System.Windows.Controls;

namespace NsqMon.Controls
{
    /// <summary>
    /// ErrorNotification
    /// </summary>
    [TemplateVisualState(Name = State.Shown, GroupName = "VisibilityStates")]
    [TemplateVisualState(Name = State.ShowDetails, GroupName = "VisibilityStates")]
    [TemplateVisualState(Name = State.Hidden, GroupName = "VisibilityStates")]
    [TemplatePart(Name = "MessageTextBlock", Type = typeof(TextBlock))]
    public class ErrorNotification : Control
    {
        private static class State
        {
            public const string Shown = "Shown";
            public const string ShowDetails = "ShowDetails";
            public const string Hidden = "Hidden";
        }

        private Button _closeButton;
        private Button _showDetailsButton;
        private TextBlock _messageTextBlock;
        private TextBlock _showDetailsButtonToolTip;
        private TextBox _detailsTextBox;
        private bool _detailsShown;
        private string _currentState = State.Hidden;
        private string _messageText;
        private string _detailsText;

        /*static ErrorNotification()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ErrorNotification),
                new FrameworkPropertyMetadata(typeof(ErrorNotification)));
        }*/

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorNotification"/> class.
        /// </summary>
        public ErrorNotification()
        {
            DefaultStyleKey = typeof(ErrorNotification);
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _messageTextBlock = (TextBlock)GetTemplateChild("MessageTextBlock");
            _showDetailsButton = (Button)GetTemplateChild("ShowDetailsButton");
            _closeButton = (Button)GetTemplateChild("CloseButton");
            _showDetailsButtonToolTip = (TextBlock)GetTemplateChild("ShowDetailsButtonToolTip");
            _detailsTextBox = (TextBox)GetTemplateChild("DetailsTextBox");

            _showDetailsButton.Click += _showDetailsButton_Click;
            _closeButton.Click += CloseButton_Click;

            _messageTextBlock.Text = _messageText;
            _detailsTextBox.Text = _detailsText;

            GoToState(_currentState);
        }

        private void GoToState(string stateName)
        {
            if (_showDetailsButtonToolTip != null)
            {
                _detailsShown = (stateName == State.ShowDetails);
                _showDetailsButtonToolTip.Text = (_detailsShown ? "Hide details" : "Show details"); // TODO: Use resource string

                VisualStateManager.GoToState(this, stateName, true);
            }

            _currentState = stateName;
        }

        private void _showDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_detailsShown)
                GoToState(State.ShowDetails);
            else
                GoToState(State.Shown);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            GoToState(State.Hidden);
        }

        /// <summary>Shows the specified exception.</summary>
        /// <param name="exception">The exception.</param>
        public void Show(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            if (_currentState != State.Hidden)
                return;

            Show(exception.Message, exception);
        }

        /// <summary>Shows the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        private void Show(string message, Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            string details = exception.ToString();

            Show(message, details);
        }

        /// <summary>Shows the specified details.</summary>
        /// <param name="details">The details.</param>
        private void Show(string details)
        {
            Show(null, details);
        }

        /// <summary>Shows the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="details">The details.</param>
        private void Show(string message, string details)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(new Action(() => Show(message, details)));
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
                _messageText = "An error has occurred.";
            else
                _messageText = string.Format("An error has occurred: {0}", message);
            _detailsText = details;

            if (_messageTextBlock != null)
                _messageTextBlock.Text = _messageText;
            if (_detailsTextBox != null)
                _detailsTextBox.Text = _detailsText;

            GoToState(State.Shown);
        }
    }
}
