using System;
using System.Globalization;
using System.Windows.Data;
using Eclipse.Models;

namespace Eclipse.Converters
{
    /// <summary>
    /// A category's name as a person would say it.
    ///
    /// The enum names read badly on screen - "PlayMode" and "ReleaseYear" are identifiers, not
    /// words - and search shows them beside every suggestion and every applied filter, where the
    /// label is what tells the user that "Sonic" the series and "Sonic Team" the developer are
    /// different things.
    ///
    /// Lives in the view layer rather than on the model, so display text stays above the boundary
    /// that keeps the search engine free of presentation.
    /// </summary>
    public class ListCategoryTypeLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ListCategoryType))
            {
                return string.Empty;
            }

            switch ((ListCategoryType)value)
            {
                case ListCategoryType.PlayMode:
                    return "play mode";

                case ListCategoryType.ReleaseYear:
                    return "year";

                case ListCategoryType.TextSearch:
                case ListCategoryType.VoiceSearch:
                case ListCategoryType.RandomGame:
                case ListCategoryType.MoreLikeThis:
                    // Not facets. Nothing filters on them, so nothing should be labelling them.
                    return string.Empty;

                default:
                    return value.ToString().ToLowerInvariant();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
