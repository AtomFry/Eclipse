using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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

    public class GameDetailOptionToBrushConverter : GameDetailOptionConverter<Brush>
    {

    }

    public class GameDetailOptionConverter<T> : IValueConverter
    {
        public GameDetailOption GameDetailComparison { get; set; }
        public T MatchingValue { get; set; }
        public T UnmatchingValue { get; set; }


        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GameDetailOption gameDetailFromBinding = (GameDetailOption)value;
            if(gameDetailFromBinding == GameDetailComparison)
            {
                return MatchingValue;
            }

            return UnmatchingValue;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class FeatureOptionConverter<T> : IValueConverter
    {
        public T PlayGameValue { get; set; }
        public T MoreInfoValue { get; set; }

        public FeatureOptionConverter(T playGameValue, T moreInfoValue)
        {
            PlayGameValue = playGameValue;
            MoreInfoValue = moreInfoValue;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is FeatureGameOption && ((FeatureGameOption)value == FeatureGameOption.PlayGame) ? PlayGameValue : MoreInfoValue;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, PlayGameValue);
        }
    }

    public sealed class FeatureOptionToDoubleConverter : FeatureOptionConverter<Double>
    {
        public FeatureOptionToDoubleConverter() : base(100, 0) { }
    }

    public class StringLengthConverter<T> : IValueConverter
    {
        public T Empty { get; set; }
        public T NonEmpty { get; set; }

        public StringLengthConverter(T EmptyValue, T NonEmptyValue)
        {
            Empty = EmptyValue;
            NonEmpty = NonEmpty;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = value as string;
            if(string.IsNullOrWhiteSpace(param))
            {
                return Empty;
            }
            else
            {
                return NonEmpty;
            }
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringLengthToVisibilityConverter : StringLengthConverter<Visibility>
    {
        public StringLengthToVisibilityConverter() : base(Visibility.Collapsed, Visibility.Visible) { }
    }

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

    public class FloatToStringConverter : FloatConverter<string>
    {
        public FloatToStringConverter() : base(string.Empty, string.Empty) { }
    }

    public class BooleanConverter<T> : IValueConverter
    {
        public T True { get; set; }
        public T False { get; set; }

        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed) { }
    }

    public sealed class BooleanToDoubleConverter : BooleanConverter<double>
    {
        public BooleanToDoubleConverter() : base(0,100) { }
    }

    public sealed class BooleanToStringConverter : BooleanConverter<string>
    {
        public BooleanToStringConverter() : base(string.Empty, string.Empty) { }
    }

    public sealed class BooleanToPointCollectionConverter : BooleanConverter<PointCollection>
    {
        public BooleanToPointCollectionConverter() : base(null, null) { }
    }

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

    // shifts the game logo down a 1/2 way when displaying feature
    public class DisplayingFeatureLogoOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool)values[0];
            bool isMoreInfo = (bool)values[1];
            double offsetAmount = (double)values[2];

            if (isMoreInfo) return 0;
            if (isFeature) return offsetAmount / 2.0;
            return 0;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("Going back is not supported");
        }
    }

    // shifts the game video and bezel up 1/2 way when displaying feature
    public class DisplayingFeatureVideoOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool)values[0];
            bool isMoreInfo = (bool)values[1];
            double offsetAmount = (double)values[2];

            if (isMoreInfo) return 0;
            if (isFeature) return -1 * offsetAmount/6.0;
            return 0;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("Going back is not supported");
        }
    }

    // toggles settings icon opacity between 100 and 10%
    // it's intended to pass in game0 which is the previous 1 game
    // the previous 1 game is null when at the start of a list
    // if previous 1 game is null, show icon at 100%, otherwise show at 10%
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
                return 0.10;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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

    // this is lame but i'm lazy and it works - sets the background row span to 10 for normal results and 16 for displaying featured game
    public class IsFeatureToRowSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool)value;
            if (isFeature) return Helpers.BackgroundRowSpanFeature;
            else return Helpers.BackgroundRowSpanNormal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // this is lame but i'm lazy and it works - sets the background column span to 20 for normal results and 32 for displaying featured game
    public class IsFeatureToColumnSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool)value;
            if (isFeature) return Helpers.BackgroundColumnSpanFeature;
            else return Helpers.BackgroundColumnSpanNormal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // this is lame but i'm lazy and it works - sets the background column to 12 for normal results and 0 for displaying featured game
    public class IsFeatureToColumnStartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFeature = (bool)value;
            if (isFeature) return Helpers.BackgroundColumnStartFeature;
            else return Helpers.BackgroundColumnStartNormal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
