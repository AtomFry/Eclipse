using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace BigBoxNetflixUI.Models
{
    public class GameList : INotifyPropertyChanged
    {
        private List<GameMatch> matchingGames;
        public List<GameMatch> MatchingGames
        {
            get { return matchingGames; }
            set
            {
                if (matchingGames != value)
                {
                    matchingGames = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("MatchingGames"));
                }
            }
        }

        public void CycleForward()
        {
            if (matchingGames != null &&
                matchingGames.Count > 0)
            {
                var games = new List<GameMatch>(MatchingGames);
                var game = games[0];
                games.RemoveAt(0);
                games.Add(game);
                MatchingGames = games;
            }
        }

        public void CycleBackward()
        {
            if (matchingGames != null &&
                matchingGames.Count > 0)
            {
                var games = new List<GameMatch>(MatchingGames);
                var game = games[matchingGames.Count - 1];
                games.RemoveAt(matchingGames.Count - 1);
                games.Insert(0, game);
                MatchingGames = games;
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

        public GameList()
        {
            MatchingGames = new List<GameMatch>();
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

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
   }
}
