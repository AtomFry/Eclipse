using System.Collections.Generic;

namespace Eclipse.Models
{
    public class EclipseSettings
    {
        public ListCategoryType DefaultListCategoryType { get; set; }
        public bool ShowGameCountInList { get; set; }

        public PageFunction PageUpFunction { get; set; }
        public PageFunction PageDownFunction { get; set; }

        public int ScreensaverDelayInSeconds { get; set; }

        public bool EnableVoiceSearch { get; set; }

        // todo: add system settings
        // enable/disable voice search
    }


    public class CustomListDefinition
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<FilterExpression> FilterExpressions { get; set; }
        public List<SortExpression> SortExpressions { get; set; }
        public int MaxGamesInList { get; set; }
        public List<ListCategoryType> ListCategoryTypes { get; set; }

        public CustomListDefinition()
        {
            SortExpressions = new List<SortExpression>();
            FilterExpressions = new List<FilterExpression>();
            ListCategoryTypes = new List<ListCategoryType>();
        }
    }

    public class FilterExpression
    {
        public GameFieldEnum GameFieldEnum { get; set; }
        public FilterFieldOperator FilterFieldOperator { get; set; }
        public object FilterFieldValue { get; set; }
    }

    public class SortExpression
    {
        public GameFieldEnum GameFieldEnum { get; set; }
        public SortDirection SortDirection { get; set; }
    }

    public enum GameFieldType
    {
        Bool,
        String,
        Float,
        Int,
        IntNullable,
        DateTime,
        DateTimeNullable
    }

    public enum GameFieldEnum
    {
        Broken,                         // Game.Broken - bool
        CommunityOrLocalStarRating,     // Game.CommunityOrLocalStarRating - float
        CommunityStarRating,            // Game.CommunityStarRating - float
        CommunityStarRatingTotalVotes,  // Game.CommunityStarRatingTotalVotes - int
        DateAdded,                      // Game.DateAdded - DateTime
        DateModified,                   // Game.DateModified - DateTime
        Developers,                     // Game.Developer
        Favorite,                       // Game.Favorite - bool
        Genres,                         // Game.GenresString
        Hide,                           // Game.Hide - bool
        LastPlayedDate,                 // Game.LastplayedDate - DateTime
        Platform,                       // Game.Platform - string
        PlayCount,                      // Game.PlayCount - int
        PlayModes,                      // Game.PlayMode
        Publishers,                     // Game.Publisher
        Rating,                         // Game.Rating - string
        Region,                         // Game.Region - string
        ReleaseDate,                    // Game.ReleaseDate - Nullable date time
        ReleaseYear,                    // Game.ReleaseYear - nullable int
        Series,                         // Game.Series
        SortTitle,                      // Game.SortTitle - string
        SortTitleOrTitle,               // Game.SortTitleOrTitle - string
        Title,                          // Game.Title - string
        Source,                         // Game.Source - string
        StarRating,                     // Game.StarRatingFloat - float
        Status,                         // Game.Status - string
        Version                         // Game.Version - string
    }

    public static class GameFieldEnumConverter
    {
        public static GameFieldType ToGameFieldType(this GameFieldEnum gameFieldEnum)
        {
            switch (gameFieldEnum)
            {
                // bool
                case GameFieldEnum.Broken:
                case GameFieldEnum.Favorite:
                case GameFieldEnum.Hide:
                    return GameFieldType.Bool;

                // string 
                case GameFieldEnum.Genres:
                case GameFieldEnum.Platform:
                case GameFieldEnum.Developers:
                case GameFieldEnum.PlayModes:
                case GameFieldEnum.Publishers:
                case GameFieldEnum.Rating:
                case GameFieldEnum.Region:
                case GameFieldEnum.Series:
                case GameFieldEnum.SortTitle:
                case GameFieldEnum.SortTitleOrTitle:
                case GameFieldEnum.Title:
                case GameFieldEnum.Source:
                case GameFieldEnum.Status:
                case GameFieldEnum.Version:
                    return GameFieldType.String;

                case GameFieldEnum.CommunityOrLocalStarRating:
                case GameFieldEnum.CommunityStarRating:
                case GameFieldEnum.StarRating:
                    return GameFieldType.Float;

                case GameFieldEnum.CommunityStarRatingTotalVotes:
                case GameFieldEnum.PlayCount:
                    return GameFieldType.Int;

                case GameFieldEnum.ReleaseYear:
                    return GameFieldType.IntNullable;

                case GameFieldEnum.DateAdded:
                case GameFieldEnum.DateModified:
                case GameFieldEnum.LastPlayedDate:
                    return GameFieldType.DateTime;

                case GameFieldEnum.ReleaseDate:
                    return GameFieldType.DateTimeNullable;

                default:
                    return GameFieldType.String;
            }
        }

        public static bool IsFilterFieldOperatorValidForField(this GameFieldEnum gameFieldEnum, FilterFieldOperator filterFieldOperator)
        {
            bool isValidOperatorForField = true;

            switch (filterFieldOperator)
            {
                case FilterFieldOperator.Contains:
                    if (gameFieldEnum.ToGameFieldType() != GameFieldType.String)
                    {
                        isValidOperatorForField = false;
                    }
                    break;

                default:
                    break;
            }

            return isValidOperatorForField;
        }

        public static string ToFieldName(this GameFieldEnum gameFieldEnum)
        {
            switch (gameFieldEnum)
            {
                case GameFieldEnum.Broken: return "Game.Broken";
                case GameFieldEnum.CommunityOrLocalStarRating: return "Game.CommunityOrLocalStarRating";
                case GameFieldEnum.CommunityStarRating: return "Game.CommunityStarRating";
                case GameFieldEnum.CommunityStarRatingTotalVotes: return "Game.CommunityStarRatingTotalVotes";
                case GameFieldEnum.DateAdded: return "Game.DateAdded";
                case GameFieldEnum.DateModified: return "Game.DateModified";
                case GameFieldEnum.Developers: return "Game.Developer";
                case GameFieldEnum.Favorite: return "Game.Favorite";
                case GameFieldEnum.Genres: return "Game.GenresString";
                case GameFieldEnum.Hide: return "Game.Hide";
                case GameFieldEnum.LastPlayedDate: return "Game.LastPlayedDate";
                case GameFieldEnum.Platform: return "Game.Platform";
                case GameFieldEnum.PlayCount: return "Game.PlayCount";
                case GameFieldEnum.PlayModes: return "Game.PlayMode";
                case GameFieldEnum.Publishers: return "Game.Publisher";
                case GameFieldEnum.Rating: return "Game.Rating";
                case GameFieldEnum.Region: return "Game.Region";
                case GameFieldEnum.ReleaseDate: return "Game.ReleaseDate";
                case GameFieldEnum.ReleaseYear: return "Game.ReleaseYear";
                case GameFieldEnum.Series: return "Game.Series";
                case GameFieldEnum.SortTitle: return "Game.SortTitle";
                case GameFieldEnum.SortTitleOrTitle: return "Game.SortTitleOrTitle";
                case GameFieldEnum.Source: return "Game.Source";
                case GameFieldEnum.StarRating: return "Game.StarRatingFloat";
                case GameFieldEnum.Status: return "Game.Status";
                case GameFieldEnum.Title: return "Game.Title";
                case GameFieldEnum.Version: return "Game.Version";
                default: return string.Empty;
            }
        }
    }

}