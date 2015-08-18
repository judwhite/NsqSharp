using System.Windows;
using System.Windows.Controls.Primitives;

namespace NsqMon.Controls
{
    /// <summary>
    /// ImageButton
    /// </summary>
    public class ImageButton : ButtonBase
    {
        /// <summary>
        /// RegularImageSource dependency property.
        /// </summary>
        public static readonly DependencyProperty RegularImageSourceProperty = DependencyProperty.Register("RegularImageSource", typeof(string), typeof(ImageButton));

        /// <summary>
        /// HotImageSource dependency property.
        /// </summary>
        public static readonly DependencyProperty HotImageSourceProperty = DependencyProperty.Register("HotImageSource", typeof(string), typeof(ImageButton));

        /// <summary>
        /// DisabledImageSource dependency property.
        /// </summary>
        public static readonly DependencyProperty DisabledImageSourceProperty = DependencyProperty.Register("DisabledImageSource", typeof(string), typeof(ImageButton));

        static ImageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton), new FrameworkPropertyMetadata(typeof(ImageButton)));
        }

        /// <summary>Gets or sets the regular image source.</summary>
        /// <value>The regular image source.</value>
        public string RegularImageSource
        {
            get { return (string)GetValue(RegularImageSourceProperty); }
            set { SetValue(RegularImageSourceProperty, value); }
        }

        /// <summary>Gets or sets the hot image source.</summary>
        /// <value>The hot image source.</value>
        public string HotImageSource
        {
            get { return (string)GetValue(HotImageSourceProperty); }
            set { SetValue(HotImageSourceProperty, value); }
        }

        /// <summary>Gets or sets the disabled image source.</summary>
        /// <value>The disabled image source.</value>
        public string DisabledImageSource
        {
            get { return (string)GetValue(DisabledImageSourceProperty); }
            set { SetValue(DisabledImageSourceProperty, value); }
        }
    }
}
