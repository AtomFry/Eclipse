using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Eclipse.Converters
{
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
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }
    }

    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() :
            base(Visibility.Visible, Visibility.Collapsed)
        { }
    }

    public sealed class BooleanToDoubleConverter : BooleanConverter<double>
    {
        public BooleanToDoubleConverter() : base(0,100)
        { }
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
            double offsetAmount = (double)values[1];

            return isFeature ? offsetAmount - offsetAmount/6.0 : 0;
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
            double offsetAmount = (double)values[1];

            return isFeature ? offsetAmount/2.0 : 0;
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
