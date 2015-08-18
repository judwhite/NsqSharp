using System.Windows;

namespace NsqMon.Common.Mvvm
{
    /// <summary>
    /// Focus Extension
    /// </summary>
    public static class FocusExtension
    {
        /// <summary>Returns <c>true</c> if the UIElement is focused; otherwise, <c>false</c>.</summary>
        /// <param name="obj">The UIElement.</param>
        /// <returns><c>true</c> if the UIElement is focused; otherwise, <c>false</c>.</returns>
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        /// <summary>Sets the <see cref="FocusExtension.IsFocusedProperty"/> attached property.</summary>
        /// <param name="obj">The UIElement.</param>
        /// <param name="value">The IsFocused value.</param>
        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        /// <summary>
        /// IsFocused attached property.
        /// </summary>
        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(FocusExtension),
                 new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        private static void OnIsFocusedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uie = (UIElement)d;
            if ((bool)e.NewValue)
            {
                uie.Focus(); // Don't care about false values.
            }
        }
    }
}
