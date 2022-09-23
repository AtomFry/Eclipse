using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Eclipse.Converters
{
    public class UserRatingToBrushConverter : IMultiValueConverter
    {
        public Color LessThanOrEqualValue { get; set; } = Colors.WhiteSmoke;
        public Color GreaterThanValue { get; set; } = Colors.Black;

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            float userRating = -1f;
            if(values[0] is float)
            {
                userRating = (float)values[0];
            }

            float offset = -1f;
            if(values[1] is float)
            {
                offset = (float)values[1];
            }


            int starNumber = -1;
            if(values[2] is int)
            {
                starNumber = (int)values[2];
            }

            offset = offset + starNumber - 1;

            if(userRating >= offset)
            {
                return GreaterThanValue;
            }

            return LessThanOrEqualValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("Going back is not supported");
        }
    }

}
