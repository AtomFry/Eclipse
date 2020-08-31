﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Unbroken.LaunchBox.Plugins.RetroAchievements;

namespace BigBoxNetflixUI.Models
{
    // a set of gamelists for browsing games by different categories (i.e. games by platform, games by genre, etc...)
    public class GameListSet
    {
        public List<GameList> GameLists { get; set; }
        public ListCategoryType ListCategoryType { get; set; }
    }

    public class GameList : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private ListCycle<GameMatch> gameCycle;

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

        public GameList(string _listDescription, List<GameMatch> _matchingGames)
        {
            ListDescription = _listDescription;
            MatchingGames = _matchingGames;
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

            Game1 = gameCycle.GetItem(1);
            Game2 = gameCycle.GetItem(2);
            Game3 = gameCycle.GetItem(3);
            Game4 = gameCycle.GetItem(4);
            Game5 = gameCycle.GetItem(5);
            Game6 = gameCycle.GetItem(6);
            Game7 = gameCycle.GetItem(7);
            Game8 = gameCycle.GetItem(8);
            Game9 = gameCycle.GetItem(9);
            Game10 = gameCycle.GetItem(10);
            Game11 = gameCycle.GetItem(11);
            Game12 = gameCycle.GetItem(12);
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

        public bool MoreImagesToLoad { get; set; } = true;
        public int CurrentGameImageIndex { get; set; } = 0;
        public void LoadNextGameImage()
        {
            // if all game images are loaded then flag it as no more images to load
            if (CurrentGameImageIndex >= MatchCount)
            {
                MoreImagesToLoad = false;
                return;
            }

            // load the next image and increment the counter
            GameMatch gameMatch = MatchingGames[CurrentGameImageIndex];
            gameMatch.SetupFrontImage();
            CurrentGameImageIndex += 1;
        }
   }
}
