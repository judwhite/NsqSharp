using System.Windows;

namespace NsqMon.Common.Wpf
{
    /// <summary>
    /// DataGridUtil
    /// </summary>
    public class DataGridUtil
    {
        /// <summary>Gets the name.</summary>
        /// <param name="obj">The dependency object.</param>
        /// <returns>The name</returns>
        public static string GetName(DependencyObject obj)
        {
            return (string)obj.GetValue(NameProperty);
        }

        /// <summary>Sets the name.</summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="value">The value.</param>
        public static void SetName(DependencyObject obj, string value)
        {
            obj.SetValue(NameProperty, value);
        }

        /// <summary>
        /// Name dependency property.
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name", typeof(string), typeof(DataGridUtil), new UIPropertyMetadata(""));
    }
}
