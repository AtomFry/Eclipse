using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
    public class BackgroundImageOversizeConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double originalWidth = 0;
            if(value is double)
            {
                originalWidth = (double)value;
            }

            return originalWidth * 1.1;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
