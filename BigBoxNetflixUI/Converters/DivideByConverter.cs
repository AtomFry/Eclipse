using BigBoxNetflixUI.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace BigBoxNetflixUI.Converters
{
    public class GameMatchToSettingsOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            GameMatch gameMatch = value as GameMatch;
            if(gameMatch == null)
            {
                return 1;
            }
            else
            {
                return 0.25;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
