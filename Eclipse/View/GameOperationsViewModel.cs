using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Concurrent;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using Eclipse.Models;
using Eclipse.Helpers;
using System.Runtime.CompilerServices;

namespace Eclipse.View
{
    public class GameOperationsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Reference to MainWindowViewModel for accessing game data and state
        private readonly MainWindowViewModel mainWindowViewModel;

        public GameOperationsViewModel(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Start the current game
        public void PlayCurrentGame()
        {
            // get a handle on the current game 
            IGame currentGame = mainWindowViewModel.CurrentGameList?.Game1?.Game;
            IAdditionalApplication additionalApplication = mainWindowViewModel.CurrentGameList?.Game1?.GameFiles?.GameVersionList?.SelectedGameVersion?.AdditionalApplication;

            if (currentGame != null)
            {
                currentGame.LastPlayedDate = DateTime.Now;

                // reset the lists so the updated history reflects - first save the current game details then reload the lists 
                mainWindowViewModel.SaveStateForGameListChange();
                mainWindowViewModel.ResetListsAfterChange();

                // stop everything in the UI
                mainWindowViewModel.CallStopVideoAndAnimationsFunction();

                mainWindowViewModel.UIState.IsPlayingGame = true;

                // launch the game 
                PluginHelper.BigBoxMainViewModel.PlayGame(currentGame, additionalApplication, null, null);
            }
        }

        // Mark current game as a favorite
        public void FavoriteCurrentGame()
        {
            GameMatch currentGame = mainWindowViewModel.CurrentGameList?.Game1;
            if (currentGame != null)
            {
                currentGame.Favorite = !currentGame.Favorite;

                PluginHelper.DataManager.Save(false);

                var gameMatchQuery = from gameMatch in mainWindowViewModel.gameBag
                                   where gameMatch.Game.Id == currentGame.Game.Id
                                   select gameMatch;

                // flag the game as a favorite wherever it appears
                foreach (GameMatch gameMatch in gameMatchQuery)
                {
                    gameMatch.Favorite = currentGame.Favorite;
                }

                // save state so we can get back to the current game
                mainWindowViewModel.SaveStateForGameListChange();
            }
        }

        // Rate the current game by a specified change amount
        public void RateCurrentGame(float changeAmount)
        {
            GameMatch currentGame = mainWindowViewModel.CurrentGameList?.Game1;
            if (currentGame != null)
            {
                float newRating = currentGame.UserRating + changeAmount;

                if (newRating > 5)
                {
                    newRating = 5.0f;
                }

                if (newRating < 0)
                {
                    newRating = 0.0f;
                }

                currentGame.UserRating = newRating;
            }
        }

        // Save the rating for the current game
        public void SaveRatingCurrentGame()
        {
            GameMatch currentGame = mainWindowViewModel.CurrentGameList?.Game1;
            if (currentGame != null)
            {
                // save the rating change to the launchbox data 
                PluginHelper.DataManager.Save(false);

                // reload the game's start rating image
                currentGame.GameFiles.ResetStarRatingImage();

                // trigger the view to update the image
                mainWindowViewModel.CallUpdateRatingImageFunction();
            }
        }
    }
}