using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace Eclipse.Models
{
    public class EclipseSettings
    {
        [DefaultValue(ListCategoryType.Platform)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public ListCategoryType DefaultListCategoryType { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AdditionalVersionsEnable { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AdditionalVersionsExcludeRunBefore { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AdditionalVersionsExcludeRunAfter { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AdditionalVersionsOnlyEmulatorOrDosBox { get; set; }

        [DefaultValue(AdditionalApplicationDisplayField.Name)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public AdditionalApplicationDisplayField AdditionalApplicationDisplayField { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AdditionalVersionsRemovePlayPrefix { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AdditionalVersionsRemoveVersionPostfix { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool EnableVoiceSearch { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool EnableScreenSaver { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowGameCountInList { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IncludeHiddenGames { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IncludeBrokenGames { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool OpenSettingsPaneOnLeft { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool DisableVideos{ get; set; }

        [DefaultValue(0.5)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public double DefaultVideoVolume { get; set; }

        [DefaultValue(PageFunction.RandomGame)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public PageFunction PageUpFunction { get; set; }

        [DefaultValue(PageFunction.VoiceSearch)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public PageFunction PageDownFunction { get; set; }

        [DefaultValue(90)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int ScreensaverDelayInSeconds { get; set; }

        [DefaultValue(2000)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int VideoDelayInMilliseconds { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool BypassDetails { get; set; }



        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowMatchPercent { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowReleaseYear { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowStarRating { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowPlayMode { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowPlatformLogo { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool RepeatGamesToFillScreen { get; set; }

        [DefaultValue(2.0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public double BoxFrontMarginLeft { get; set; }

        [DefaultValue(2.0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public double BoxFrontMarginRight { get; set; }

        [DefaultValue(2.0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public double BoxFrontMarginTop { get; set; }

        [DefaultValue(2.0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public double BoxFrontMarginBottom { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool DisplayFeaturedGame { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowOptionsIcon { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool DisplayOptionsOnEscape { get; set; }
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

    public enum AdditionalApplicationDisplayField
    {
        Name,
        Version,
        Region
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