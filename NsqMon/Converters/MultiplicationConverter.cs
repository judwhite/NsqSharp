using System;
using System.Windows.Data;

namespace NsqMon.Converters
{
    public class MultiplicationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string param = (string)parameter;
            if (param.Contains(","))
            {
                string[] parameters = param.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                double product = (double)value * double.Parse(parameters[0]);
                return product + double.Parse(parameters[1]);
            }
            else
            {
                return (double)value * double.Parse(param);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
