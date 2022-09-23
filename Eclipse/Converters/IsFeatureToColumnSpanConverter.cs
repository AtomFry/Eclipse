using Eclipse.Helpers;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
    // this is lame but i'm lazy and it works - sets the background column span to 20 for normal results and 32 for displaying featured game
    public class IsFeatureToColumnSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool)value;
            if (isFeature) return EclipseConstants.BackgroundColumnSpanFeature;
            else return EclipseConstants.BackgroundColumnSpanNormal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
