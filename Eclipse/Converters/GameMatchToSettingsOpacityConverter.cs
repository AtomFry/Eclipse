using Eclipse.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
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

}
