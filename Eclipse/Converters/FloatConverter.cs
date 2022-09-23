using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
    public class FloatConverter<T> : IValueConverter
    {
        public T ZeroValue { get; set; }
        public T NonZeroValue { get; set; }
        public FloatConverter(T zeroValue, T nonZeroValue)
        {
            ZeroValue = zeroValue;
            NonZeroValue = nonZeroValue;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float parameterValue = (float)value;
            if (parameterValue <= 0) { return ZeroValue; }
            return NonZeroValue;
        }
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
