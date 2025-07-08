using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Eclipse.Models;
using Eclipse.View;

namespace Eclipse.Service
{
    public class RandomGameSelectionService : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Reference to MainWindowViewModel for accessing game data and state
        private readonly MainWindowViewModel mainWindowViewModel;

        private static readonly Random random = new Random();

        private GameMatch attractModeGame;
        public GameMatch AttractModeGame
        {
            get => attractModeGame;
            set
            {
                if (attractModeGame != value)
                {
                    attractModeGame = value;
                    OnPropertyChanged();
                }
            }
        }

        public RandomGameSelectionService(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void DoRandomGame(int randomIndex = -1)
        {
            // get a game index from the current list set
            if (randomIndex == -1)
            {
                randomIndex = random.Next(0, mainWindowViewModel.CurrentGameListSet.TotalGameCount);
            }

            // find the index of which list it's in 
            for (int listIndex = 0; listIndex < mainWindowViewModel.CurrentGameListSet.GameLists.Count; listIndex++)
            {
                GameList gameList = mainWindowViewModel.CurrentGameListSet.GameLists[listIndex];

                if (gameList.ListSetStartIndex <= randomIndex && gameList.ListSetEndIndex >= randomIndex)
                {
                    // once found, cycle to that list
                    mainWindowViewModel.listCycle.SetCurrentIndex(listIndex);

                    // refresh the game lists so we can get a handle on the current list
                    mainWindowViewModel.CurrentGameList = mainWindowViewModel.listCycle.GetItem(0);
                    mainWindowViewModel.NextGameList = mainWindowViewModel.listCycle.GetItem(1);

                    // setup the game list to the random game index 
                    mainWindowViewModel.CurrentGameList.SetGameIndex(randomIndex - gameList.ListSetStartIndex);
                    break;
                }
            }

            // call the game change function to refresh things
            mainWindowViewModel.CallGameChangeFunction();
        }

        public void NextAttractModeGame()
        {
            int randomIndex = random.Next(mainWindowViewModel.gameBag.Count);
            AttractModeGame = mainWindowViewModel.gameBag.ElementAt(randomIndex);
        }
    }
}