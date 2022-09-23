using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Models
{
    public class GameMatch : INotifyPropertyChanged
    {
        #region Static Members

        public static string IEnumerableToCommaSeparatedString(IEnumerable<string> list)
        {
            if (list != null)
            {
                StringBuilder sb = new StringBuilder();

                foreach (string item in list)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(item);
                }
                return sb.ToString();
            }
            return string.Empty;
        }


        public static string ResolveDeveloperString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.Developers);
        }

        public static string ResolvePublisherString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.Publishers);
        }

        public static string ResolveSeriesString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.SeriesValues);
        }

        public static string ResolveGenreString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.Genres);
        }

        public static string ResolveReleaseYear(IGame Game)
        {
            int? releaseYear = Game?.ReleaseDate?.Year;
            if(releaseYear != null)
            {
                return releaseYear.ToString();
            }
            return "    ";
        }

        public static int ResolveDefaultMatchPercentage(IGame Game)
        {
            float? communityOrLocalStarRating = Game?.CommunityOrLocalStarRating;
            if(communityOrLocalStarRating != null)
            {
                return (int)(communityOrLocalStarRating * 20);
            }
            return 0;
        }
        #endregion

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public IGame Game { get; set; }

        private GameFiles gameFiles;
        public GameFiles GameFiles
        {
            get { return gameFiles; } 
            set
            {
                if(gameFiles != value)
                {
                    gameFiles = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("GameFiles"));
                }
            }
        }

        public TitleMatchType TitleMatchType { get; set; }

        public string ConvertedTitle { get; set; }


        // expose IGame favorite setting so we can save it back to the file
        public bool Favorite
        {
            get { return Game.Favorite; }
            set
            {
                if (Game.Favorite != value)
                {
                    Game.Favorite = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Favorite"));
                }
            }
        }

        // expose IGame user rating setting so we can save it back to the file
        public float UserRating
        {
            get { return Game.StarRatingFloat; }
            set
            {
                if (Game.StarRatingFloat != value)
                {
                    Game.StarRatingFloat = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("UserRating"));
                }
            }
        }

        public string Developer { get; set; }
        public string Publisher { get; set; }
        public string Series { get; set; }
        public string Genre { get; set; }
        public string ReleaseYear { get; set; }

        // set the match percentage based on star rating unless voice match then use voice recognition details
        public int MatchPercentage { get; set; }
        public string MatchDescription { get; set; }

        public ListCategoryType CategoryType { get; set; }
        public string CategoryValue { get; set; }
        #endregion

        public GameMatch()
        {

        }

        public GameMatch(IGame game, GameFiles gameFiles)
        {
            Game = game;
            GameFiles = gameFiles;

            Developer = ResolveDeveloperString(game);
            Publisher = ResolvePublisherString(game);
            Series = ResolveSeriesString(game);
            Genre = ResolveGenreString(game);
            ReleaseYear = ResolveReleaseYear(game);
            MatchPercentage = ResolveDefaultMatchPercentage(game);
            SetMatchDescription();
        }

        public static GameMatch CloneGameMatch(GameMatch otherGameMatch,
                                               ListCategoryType categoryType,
                                               string categoryValue,
                                               TitleMatchType titleMatchType = TitleMatchType.None,
                                               string convertedTitle = "")
        {
            GameMatch gameMatch = new GameMatch();
            gameMatch.Game = otherGameMatch.Game;
            gameMatch.GameFiles = otherGameMatch.GameFiles;
            gameMatch.TitleMatchType = titleMatchType;
            gameMatch.CategoryType = categoryType;
            gameMatch.CategoryValue = categoryValue;
            gameMatch.ConvertedTitle = convertedTitle;

            gameMatch.Developer = otherGameMatch.Developer;
            gameMatch.Publisher = otherGameMatch.Publisher;
            gameMatch.Series = otherGameMatch.Series;
            gameMatch.Genre = otherGameMatch.Genre;
            gameMatch.ReleaseYear = otherGameMatch.ReleaseYear;

            gameMatch.MatchPercentage = otherGameMatch.MatchPercentage;
            gameMatch.MatchDescription = otherGameMatch.MatchDescription;
            return gameMatch;
        }

        private void SetMatchDescription()
        {
            MatchDescription = $"{string.Format("{0:00}", MatchPercentage)}% Match";
        }

        public void SetupVoiceMatchPercentage(float confidence, string phrase)
        {
            /*
             * For voice matches...
             * Confidence of spoken phrase (0-1) - this will be multiplied by the final result 
             * 
             * Match level: full title (100), main title (95), subtitle (90), phrase (60)
             * Bonus portion allows the percentage of the title that was matched (length of phrase / length of title) to be applied to available score
             * 
             * Examples 
             * 1.  Full title match is 100 so there is no room for bonus
             * 2.  Main title match is 95 so there are 5 bonus points available.  
             * 3.  Title contains is 60 so there is 40 bonus points available.  For the phrase "super mario" against the game "super mario bros." you get bonus 40 * (11/17)

             * Multiply the confidence by the final score 
             * 
             * Feels like this shit could be tweaked endlessly and never settle on an ideal solution?
             */
            var lengthMatchPercent = (float)((float)phrase.Length / (float)ConvertedTitle.Length);

            int availableBonus = 100 - (int)TitleMatchType;
            float bonusMatchTypePortion = availableBonus * lengthMatchPercent;

            // subtracting 0.001 to make sure it's never 100% - probably impossible since the confidence is never 1 but just in case
            var matchTypePortion = confidence * ((int)TitleMatchType + bonusMatchTypePortion - 0.001);
            MatchPercentage = (int)matchTypePortion;
            SetMatchDescription();
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                GameMatch other = obj as GameMatch;
                if (other == null)
                {
                    return (false);
                }

                return (Game.Id == other.Game.Id);
            }
        }

        public override int GetHashCode()
        {
            return (Game.Id.GetHashCode());
        }
    }
}
