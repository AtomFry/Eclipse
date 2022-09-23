using System;
using System.Windows.Data;

namespace Eclipse.Converters
{
    // converter to divide value x by value a
    public class DivideByConverter : IValueConverter
    {
        public double A { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double a = GetDoubleValue(parameter, A);
            double x = GetDoubleValue(value, 0.0);
            return (x / a);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double a = GetDoubleValue(parameter, A);
            double y = GetDoubleValue(value, 0.0);
            return (a / y);
        }

        private double GetDoubleValue(object parameter, double defaultValue)
        {
            double a;
            if (parameter != null)
                try
                {
                    a = System.Convert.ToDouble(parameter);
                }
                catch
                {
                    a = defaultValue;
                }
            else
                a = defaultValue;
            return a;
        }
    }
}
