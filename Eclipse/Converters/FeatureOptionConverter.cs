using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
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

}
