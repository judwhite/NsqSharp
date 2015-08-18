using System;
using System.Windows.Data;

namespace NsqMon.Converters
{
    public class AllTrueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                foreach (bool value in values)
                {
                    if (!value)
                        return false;
                }

                return true;
            }
            catch
            {
                // TODO: Clean this up
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
