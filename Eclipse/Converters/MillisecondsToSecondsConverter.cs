using System;
using System.Windows.Data;

namespace Eclipse.Converters
{
    // Screen saver timings are stored in milliseconds for precision, but a 17 second pan
    // reads better than 17000. Display only - the settings themselves stay in milliseconds.
    public class MillisecondsToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return System.Convert.ToDouble(value, culture) / 1000.0;
            }
            catch
            {
                return 0.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return (int)Math.Round(System.Convert.ToDouble(value, culture) * 1000.0);
            }
            catch
            {
                return 0;
            }
        }
    }
}
