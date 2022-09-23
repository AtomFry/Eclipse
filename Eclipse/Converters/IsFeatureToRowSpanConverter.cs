using Eclipse.Helpers;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
    // this is lame but i'm lazy and it works - sets the background row span to 10 for normal results and 16 for displaying featured game
    public class IsFeatureToRowSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool)value;
            if (isFeature) return EclipseConstants.BackgroundRowSpanFeature;
            else return EclipseConstants.BackgroundRowSpanNormal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
