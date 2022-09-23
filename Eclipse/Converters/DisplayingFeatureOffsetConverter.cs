using System;
using System.Windows.Data;

namespace Eclipse.Converters
{
    // specifies the amount to offset the game list
    // intended to take isFeature as a parameter and the height of the gamelist
    // if displaying the feature game then we offset the gamelist by the height of the gamelist
    // if not displaying the feature game then offset is 0
    public class DisplayingFeatureOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool) values[0];
            bool isMoreInfo = (bool)values[1];
            double offsetAmount = (double)values[2];

            if (isMoreInfo) return offsetAmount * 2;

            if (isFeature) return offsetAmount;

            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("Going back is not supported");
        }
    }

}
