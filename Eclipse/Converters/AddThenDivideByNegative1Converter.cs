using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
    public class AddThenDivideByNegative1Converter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 
                && values[0] is double width 
                && values[1] is double leftMargin
                && values[2] is double rightMargin)
            {
                return (width + leftMargin + rightMargin) / -1;
            }

            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
