using System;
using System.Globalization;
using System.Windows.Data;

namespace NsqMon.Converters
{
    public class IsEnumEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string.Format("{0}", value) == (string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
