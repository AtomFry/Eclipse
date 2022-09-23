using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
    public class BooleanConverter<T> : IValueConverter
    {
        public T True { get; set; }
        public T False { get; set; }

        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
