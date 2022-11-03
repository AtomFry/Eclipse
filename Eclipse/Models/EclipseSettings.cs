using System.Collections.Generic;

namespace Eclipse.Models
{
    public class EclipseSettings
    {
        public ListCategoryType DefaultListCategoryType { get; set; }
        public bool ShowGameCountInList { get; set; }

        // todo: add system settings
        // enable/disable voice search
        // page up behavior
        // page down behavior
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
        StringArray,
        StringBlockingCollection,
        Float,
        Int,
        IntNullable,
        DateTime,
        DateTimeNullable
    }

    public enum GameFieldEnum
    {
        Broken,         // Game.Broken - bool

        CommunityOrLocalStarRating,     // Game.CommunityOrLocalStarRating - float
        CommunityStarRating,            // Game.CommunityStarRating - float
        CommunityStarRatingTotalVotes,  // Game.CommunityStarRatingTotalVotes - int

        DateAdded,              // Game.DateAdded - DateTime
        DateModified,           // Game.DateModified - DateTime
        Developers,         // Game.Developers - string array
        DeveloperString,    // Game.Developer - string

        Favorite,       // Game.Favorite - bool

        Genres,             // Game.Genres - string blocking collection
        GenreString,    // Game.GenresString - string

        Hide,           // Game.Hide - bool

        LastPlayedDate,         // Game.LastplayedDate - DateTime

        Platform,       // Game.Platform - string
        PlayCount,                      // Game.PlayCount - int
        PlayModes,          // Game.PlayModes - string array
        PlayModeString,     // Game.PlayMode - string
        Publisher,          // Game.Publisher - string
        Publishers,         // Game.Publishers - string array

        Rating,             // Game.Rating - string
        Region,             // Game.Region - string
        ReleaseDate,             // Game.ReleaseDate - Nullable date time
        ReleaseYear,                // Game.ReleaseYear - nullable int

        SeriesString,       // Game.Series - string
        SortTitle,          // Game.SortTitle - string
        SortTitleOrTitle,   // Game.SortTitleOrTitle - string

        Title,              // Game.Title - string

        Series,             // Game.SeriesValues - string array
        Source,             // Game.Source - string
        StarRating,                     // Game.StarRatingFloat - float
        Status,             // Game.Status - string

        Version            // Game.Version - string
    }
}