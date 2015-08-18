using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace NsqMon.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is IEnumerable)
            {
                ObservableCollection<string> list = new ObservableCollection<string>();
                foreach (var item in (IEnumerable)value)
                {
                    list.Add(GetEnumString(item));
                }

                return list;
            }
            else
            {
                return GetEnumString(value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;

            Type[] types = targetType.GetGenericArguments();
            if (types.Length == 1) // handle nullable enum
                targetType = types[0];

            foreach (FieldInfo fieldInfo in targetType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                object[] descriptions = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descriptions.Length == 1)
                {
                    if (((DescriptionAttribute)descriptions[0]).Description == value.ToString())
                        return fieldInfo.GetValue(null);
                }
                else
                {
                    object enumValue = fieldInfo.GetValue(null);
                    if (enumValue.ToString() == value.ToString())
                        return enumValue;
                }
            }

            return null;
        }

        private static string GetEnumString(object item)
        {
            string enumString = item.ToString();

            object[] descriptions = item.GetType().GetField(enumString).GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (descriptions.Length == 1)
                return ((DescriptionAttribute)descriptions[0]).Description;
            else
                return enumString;
        }
    }
}
