using Eclipse.Service;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace Eclipse.Models
{
    // a set of gamelists for browsing games by different categories (i.e. games by platform, games by genre, etc...)
    public class GameListSet
    {
        private List<GameList> gameLists;
        public List<GameList> GameLists
        {
            get
            {
                return gameLists;
            }
            set
            {
                gameLists = value;

                // when the collection of lists are set, setup their start/end index - used for random searches
                int nextStart = 0;
                foreach (GameList gameList in gameLists)
                {
                    gameList.ListSetStartIndex = nextStart;
                    nextStart = gameList.ListSetEndIndex + 1;
                }
            }
        }
        public ListCategoryType ListCategoryType { get; set; }

        // get total count of games across all lists
        private int? totalGameCount;
        public int TotalGameCount
        {
            get
            {
                if (totalGameCount == null)
                {
                    totalGameCount = gameLists.Sum(gameList => gameList.MatchCount);
                }

                return totalGameCount.GetValueOrDefault();
            }
        }
    }

    public class GameList : INotifyPropertyChanged
    {
        private EclipseSettings EclipseSettings = EclipseSettingsDataProvider.Instance.EclipseSettings;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private ListCycle<GameMatch> gameCycle;

        public int SortOrder { get; set; }

        private List<GameMatch> matchingGames;
        public List<GameMatch> MatchingGames
        {
            get { return matchingGames; }
            set
            {
                if (matchingGames != value)
                {
                    matchingGames = value;

                    gameCycle = new ListCycle<GameMatch>(MatchingGames, 13);
                    gameCycle.CycleBackward();
                    RefreshGames();

                    PropertyChanged(this, new PropertyChangedEventArgs("MatchingGames"));
                }
            }
        }

        private ListCategoryType listCategoryType;
        public ListCategoryType ListCategoryType
        {
            get { return listCategoryType; }
            set
            {
                if (listCategoryType != value)
                {
                    listCategoryType = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ListCategoryType"));
                }
            }
        }

        // to be used with selecting random games and reset the game cycle to the right spot
        public void SetGameIndex(int newIndex)
        {
            gameCycle.SetCurrentIndex(newIndex, true);
            RefreshGames();
        }

        public int CurrentGameIndex
        {
            get
            {
                return gameCycle.GetIndexValue(1);
            }
        }

        public GameList()
        {
        }

        public GameList(string _listDescription, List<GameMatch> _matchingGames, int sortOrder = 9999)
        {
            ListTypeValue = _listDescription;
            MatchingGames = _matchingGames;
            SortOrder = sortOrder;

            ListDescription = ListTypeValue;
            if (EclipseSettingsDataProvider.Instance.EclipseSettings.ShowGameCountInList)
            {
                ListDescription = $"{ListDescription} ({MatchCount})";
            }
        }

        public void CycleForward()
        {
            gameCycle.CycleForward();
            RefreshGames();
        }

        public void CycleBackward()
        {
            gameCycle.CycleBackward();
            RefreshGames();
        }

        private void RefreshGames()
        {
            // at the start of the list, reset the previous game at index 0 to null
            if (gameCycle.GetIndexValue(1) == 0)
            {
                Game0 = null;
            }
            else
            {
                Game0 = gameCycle.GetItem(0);
            }

            Game1 = (MatchCount > 0) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(1) : null;
            Game2 = (MatchCount > 1) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(2) : null;
            Game3 = (MatchCount > 2) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(3) : null;
            Game4 = (MatchCount > 3) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(4) : null;
            Game5 = (MatchCount > 4) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(5) : null;
            Game6 = (MatchCount > 5) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(6) : null;
            Game7 = (MatchCount > 6) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(7) : null;
            Game8 = (MatchCount > 7) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(8) : null;
            Game9 = (MatchCount > 8) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(9) : null;
            Game10 = (MatchCount > 9) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(10) : null;
            Game11 = (MatchCount > 10) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(11) : null;
            Game12 = (MatchCount > 11) || (EclipseSettings.RepeatGamesToFillScreen) ? gameCycle.GetItem(12) : null;
        }

        private string listTypeValue;
        public string ListTypeValue
        {
            get { return listTypeValue; }
            set
            {
                if (listTypeValue != value)
                {
                    listTypeValue = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ListTypeValue"));
                }
            }
        }

        private string listDescription;
        public string ListDescription
        {
            get { return listDescription; }
            set
            {
                if (listDescription != value)
                {
                    listDescription = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ListDescription"));
                }
            }
        }

        private float confidence;
        public float Confidence
        {
            get => confidence;
            set
            {
                if (confidence != value)
                {
                    confidence = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Confidence"));
                }
            }
        }

        public TitleMatchType MaxTitleMatchType
        {
            get
            {
                if (MatchingGames == null)
                    return TitleMatchType.None;

                return MatchingGames.Max(game => game.TitleMatchType);
            }
        }

        public float? MaxMatchPercentage
        {
            get
            {
                if (MatchingGames == null)
                    return 0;

                return MatchingGames.Max(game => game.MatchPercentage);
            }
        }

        public int MaxTitleLength
        {
            get
            {
                if (MatchingGames == null)
                    return 0;

                // get max title length for games having the maximum match percentage
                return matchingGames.Where(game => game.MatchPercentage == MaxMatchPercentage)
                                    .Max(game => game.Game.Title.Length);
            }
        }

        public Brush Brush
        {
            get
            {
                if (Confidence <= 0.50)
                    return new SolidColorBrush(Colors.Red);

                if (Confidence <= 0.60)
                    return new SolidColorBrush(Colors.Orange);

                if (Confidence <= 0.70)
                    return new SolidColorBrush(Colors.Yellow);

                if (Confidence <= 0.80)
                    return new SolidColorBrush(Colors.YellowGreen);

                if (Confidence <= 0.90)
                    return new SolidColorBrush(Colors.GreenYellow);

                return new SolidColorBrush(Colors.Green);
            }
        }

        public int MatchCount
        {
            get
            {
                if (MatchingGames == null)
                {
                    return 0;
                }

                return MatchingGames.Count();
            }
        }

        public int ListSetStartIndex { get; set; }
        public int ListSetEndIndex
        {
            get
            {
                return ListSetStartIndex + MatchCount - 1;
            }
        }

        private GameMatch game0;
        public GameMatch Game0
        {
            get { return game0; }
            set
            {
                if (game0 != value)
                {
                    game0 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game0"));
                }
            }
        }

        private GameMatch game1;
        public GameMatch Game1
        {
            get { return game1; }
            set
            {
                if (game1 != value)
                {
                    game1 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game1"));
                }
            }
        }

        private GameMatch game2;
        public GameMatch Game2
        {
            get { return game2; }
            set
            {
                if (game2 != value)
                {
                    game2 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game2"));
                }
            }
        }

        private GameMatch game3;
        public GameMatch Game3
        {
            get { return game3; }
            set
            {
                if (game3 != value)
                {
                    game3 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game3"));
                }
            }
        }

        private GameMatch game4;
        public GameMatch Game4
        {
            get { return game4; }
            set
            {
                if (game4 != value)
                {
                    game4 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game4"));
                }
            }
        }

        private GameMatch game5;
        public GameMatch Game5
        {
            get { return game5; }
            set
            {
                if (game5 != value)
                {
                    game5 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game5"));
                }
            }
        }

        private GameMatch game6;
        public GameMatch Game6
        {
            get { return game6; }
            set
            {
                if (game6 != value)
                {
                    game6 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game6"));
                }
            }
        }

        private GameMatch game7;
        public GameMatch Game7
        {
            get { return game7; }
            set
            {
                if (game7 != value)
                {
                    game7 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game7"));
                }
            }
        }

        private GameMatch game8;
        public GameMatch Game8
        {
            get { return game8; }
            set
            {
                if (game8 != value)
                {
                    game8 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game8"));
                }
            }
        }

        private GameMatch game9;
        public GameMatch Game9
        {
            get { return game9; }
            set
            {
                if (game9 != value)
                {
                    game9 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game9"));
                }
            }
        }

        private GameMatch game10;
        public GameMatch Game10
        {
            get { return game10; }
            set
            {
                if (game10 != value)
                {
                    game10 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game10"));
                }
            }
        }

        private GameMatch game11;
        public GameMatch Game11
        {
            get { return game11; }
            set
            {
                if (game11 != value)
                {
                    game11 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game11"));
                }
            }
        }

        private GameMatch game12;
        public GameMatch Game12
        {
            get { return game12; }
            set
            {
                if (game12 != value)
                {
                    game12 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game12"));
                }
            }
        }
    }
}
