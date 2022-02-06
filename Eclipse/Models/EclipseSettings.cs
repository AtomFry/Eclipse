using System.Collections.Generic;

namespace Eclipse.Models
{
    public class EclipseSettings
    {
        // turn random game function on/off
        public bool EnableRandomGame { get; set; }     

        // turn voice search on/off
        public bool EnableVoiceSearch { get; set; }    

        // delay in seconds until attract mode starts
        public int AttractModeDelayInSeconds { get; set; }       
        
        // configure maximum number of games for the history list - leave null for no maximum
        // configure how far back in time to get games from - leave null for no filter
        public DateBasedListSettings HistorySettings { get; set; }

        // configure maximum number of games for the recently added list - leave null for no maximum
        // configure how far back in time to get games from - leave null for no filter
        public DateBasedListSettings RecentlyAddedSettings { get; set; }
        
        // configure maximum number of games for the never played list - leave null for no maximum
        public AdditionalListSettings NeverPlayedSettings { get; set; }

        // configure maximum number of games for the top rated by user list - leave null for no maximum (all games)
        // configure minimum rating to include a game in the list - leave null for no minimum (all games)
        public RatingBasedListSettings TopRatedByUserSettings { get; set; }

        // configure maximum number of games for the top rated by community list - leave null for no maximum (all games)
        // configure minimum rating to include a game in the list - leave null for no minimum (all games)
        // configure minimum number of votes to include a gam in the list - leave null for no minimum (all games)
        public RatingBasedListSettings TopRatedByCommunitySettings { get; set; }

        // list of list settings - specify for each list if you want to include favorites, history, recently added, etc...
        public List<ListSetSettings> SetOfListSetSettings { get; set; }
    }

    public class ListSetSettings
    {
        public ListCategoryType CategoryType { get; set; }
        public bool IncludeFavorites { get; set; }
        public bool IncludeHistory { get; set; }
        public bool IncludeRecentlyAdded { get; set; }
        public bool IncludeNeverPlayed { get; set; }
        public bool IncludeTopRatedByUser { get; set; }
        public bool IncludeTopRatedByCommunity { get; set; }
    }

    public class AdditionalListSettings
    {
        public int? MaxGamesInList { get; set; }
    }

    public class DateBasedListSettings : AdditionalListSettings
    {
        public int? NumberOfDays { get; set; }
    }

    public class RatingBasedListSettings : AdditionalListSettings
    {
        public float? MinimumRating { get; set; }
        public int? MinimumNumberOfVotes { get; set; }
    }
}
