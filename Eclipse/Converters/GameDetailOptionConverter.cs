using Eclipse.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
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
}
