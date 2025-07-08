using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using System.Data;
using Eclipse.Models;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Eclipse.Helpers;
using System.Threading;
using Eclipse.State;
using System.Linq.Expressions;
using System.Reflection;
using Eclipse.Service;
using System.Runtime.CompilerServices;

namespace Eclipse.View
{
    public delegate void AnimateGameChangeFunction();
    public delegate void IncrementLoadingProgressFunction();
    public delegate void StopVideoAndAnimations();
    public delegate void UpdateRatingImage();

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ListCycle<GameList> listCycle;
        internal List<GameListSet> GameListSets;
        internal ConcurrentBag<GameMatch> gameBag;
        internal ConcurrentBag<GameFiles> gameFilesBag;

        public VideoControlViewModel VideoControl { get; private set; }
        public GameDetailsViewModel GameDetails { get; private set; }
        public UIStateViewModel UIState { get; private set; }
        public GameOperationsViewModel GameOperations { get; private set; }
        public GameListManagementService GameListManagement { get; private set; }
        public FileProcessingService FileProcessing { get; private set; }
        public RandomGameSelectionService RandomGameSelection { get; private set; }

        private GameDetailOption gameDetailOption;
        private string errorMessage;

        public EclipseStateContext EclipseStateContext { get; set; }

        private Models.EclipseSettings eclipseSettings;

        public MainWindowViewModel()
        {
            UIState = new UIStateViewModel
            {
                IsInitializing = true
            };

            FeatureOption = FeatureGameOption.PlayGame;

            VideoControl = new VideoControlViewModel();

            InitializeEclipseSettings();

            GameDetails = new GameDetailsViewModel(eclipseSettings);
            GameOperations = new GameOperationsViewModel(this);
            GameListManagement = new GameListManagementService(this);
            FileProcessing = new FileProcessingService(this);
            RandomGameSelection = new RandomGameSelectionService(this);

            EclipseStateContext = new EclipseStateContext(this);
        }

        public void InitializeEclipseSettings()
        {
            eclipseSettings = EclipseSettingsDataProvider.Instance?.EclipseSettings;
            VideoControl.VideoVolume = eclipseSettings?.DefaultVideoVolume ?? 0.5;

            double marginLeft = eclipseSettings?.BoxFrontMarginLeft ?? 2;
            double marginRight = eclipseSettings?.BoxFrontMarginRight ?? 2;
            double marginTop = eclipseSettings?.BoxFrontMarginTop ?? 2;
            double marginBottom = eclipseSettings?.BoxFrontMarginBottom ?? 2;

            double selectedGameDetailsPadding = eclipseSettings?.SelectedGameDetailsPadding ?? 0;

            FrontImageMargin = new System.Windows.Thickness(marginLeft, marginTop, marginRight, marginBottom);
            SelectedGameDetailsPadding = new System.Windows.Thickness(selectedGameDetailsPadding);
        }



        public GameDetailOption GameDetailOption
        {
            get => gameDetailOption;
            set
            {
                {
                    gameDetailOption = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                if (errorMessage != value)
                {
                    errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public AnimateGameChangeFunction GameChangeFunction { get; set; }
        public StopVideoAndAnimations StopVideoAndAnimationsFunction { get; set; }
        public UpdateRatingImage UpdateRatingImageFunction { get; set; }

        private GameListSet currentGameListSet;
        public GameListSet CurrentGameListSet
        {
            get { return currentGameListSet; }
            set
            {
                if (currentGameListSet != value)
                {
                    currentGameListSet = value;
                    OnPropertyChanged();
                }
            }
        }

        private OptionList optionList;
        public OptionList OptionList
        {
            get => optionList;
            set
            {
                if (optionList != value)
                {
                    optionList = value;
                    OnPropertyChanged();
                }
            }
        }


        public void SetupFiles(object sender, DoWorkEventArgs e)
        {
            FileProcessing.SetupFiles(sender, e);
        }

        public void CreateGameLists()
        {
            GameListManagement.CreateGameLists();
        }

        public void DoMoreLikeCurrentGame()
        {
            GameListManagement.DoMoreLikeCurrentGame();
        }

        public GameMatch AttractModeGame => RandomGameSelection.AttractModeGame;

        public void NextAttractModeGame()
        {
            RandomGameSelection.NextAttractModeGame();
        }


        public void CallGameChangeFunction()
        {
            GameChangeFunction?.Invoke();
        }

        public void CallStopVideoAndAnimationsFunction()
        {
            StopVideoAndAnimationsFunction?.Invoke();
        }

        internal void CallUpdateRatingImageFunction()
        {
            UpdateRatingImageFunction?.Invoke();
        }


        public void CycleListBackward()
        {
            GameListManagement.CycleListBackward();
        }

        public void CycleListForward()
        {
            GameListManagement.CycleListForward();
        }

        public bool DoUp(bool held)
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnUp(held);
        }

        public bool DoDown(bool held)
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnDown(held);
        }

        public bool DoLeft(bool held)
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnLeft(held);
        }

        public bool DoRight(bool held)
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnRight(held);
        }

        public bool DoPageUp()
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnPageUp();
        }

        public bool DoPageDown()
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnPageDown();
        }

        public void ResetGameLists(ListCategoryType listCategoryType)
        {
            // get the game list from the GameListSet for the given listCategoryType
            IEnumerable<GameListSet> query = from gameListSet in GameListSets
                                             where gameListSet.ListCategoryType == listCategoryType
                                             select gameListSet;

            CurrentGameListSet = query?.FirstOrDefault();
            if (CurrentGameListSet != null)
            {
                listCycle = new ListCycle<GameList>(CurrentGameListSet.GameLists, 2);
                GameListManagement.RefreshGameLists();
            }
        }

        public void DoRandomGame(int randomIndex = -1)
        {
            RandomGameSelection.DoRandomGame(randomIndex);
        }

        // start the current game
        public void PlayCurrentGame()
        {
            GameOperations.PlayCurrentGame();
        }

        // mark current game as a favorite
        public void FavoriteCurrentGame()
        {
            GameOperations.FavoriteCurrentGame();
        }

        // variables to track what list set, list, and game we were on when a game is favorited
        // save current list set type - i.e. lists by platform, genre, publisher, etc...
        // save the current list type - generally would match the list set unless it's favorites
        // identifies which list we are in within the list set - would be better if we created a guid to identify these
        // get the game id that we are on 
        // get the starting index for the list within the list set
        // get the index of the game within the list 
        internal bool gameListsChanged;
        private ListCategoryType preChangeListSetCategoryType;
        private ListCategoryType preChangeListCategoryType;
        private string preChangeListDescription;
        private string preChangeGameId;
        private int preChangeGameIndex;

        // call this when lists are about to change to save which list set, list, and game we were on so we can find our way back after rebuilding lists
        // this is needed when game lists are going to change (i.e. adding/removing favorites, adding/removing from history)
        internal void SaveStateForGameListChange()
        {
            // flag the favorites list has changed 
            gameListsChanged = true;

            // save current list set type, list type, list description, 
            // save current list set type - i.e. lists by platform, genre, publisher, etc...
            preChangeListSetCategoryType = currentGameListSet.ListCategoryType;

            // save the current list type - generally would match the list set unless it's favorites
            preChangeListCategoryType = currentGameList.ListCategoryType;

            // identifies which list we are in within the list set - would be better if we created a guid to identify these
            preChangeListDescription = currentGameList.ListDescription;

            // get the game id that we are on 
            preChangeGameId = currentGameList.Game1.Game.Id;

            // get the index of the game within the list 
            preChangeGameIndex = currentGameList.CurrentGameIndex;
        }

        public void CheckResetGameLists()
        {
            GameListManagement.CheckResetGameLists();
        }

        // call this when lists have changed (i.e. game added/removed from favorites history list)
        // will try to find the game in the same list - if it can't (i.e. in favorites and game removed from favorites) then jumps to the next game
        internal void ResetListsAfterChange()
        {
            // clear the game list changed flag 
            gameListsChanged = false;

            // recreate the lists
            CreateGameLists();

            // reset to the list set that we were on 
            ResetGameLists(preChangeListSetCategoryType);

            // find the list that we were previously in 
            var priorListQuery = from list in currentGameListSet.GameLists
                                 where list.ListCategoryType == preChangeListCategoryType && list.ListDescription == preChangeListDescription
                                 select list;

            var gameList = priorListQuery?.FirstOrDefault();
            if (gameList != null)
            {
                // try to find the game in the list
                var gameMatchQuery = from match in gameList.MatchingGames
                                     where match.Game.Id == preChangeGameId
                                     select match;

                var gameMatch = gameMatchQuery?.FirstOrDefault();
                if (gameMatch != null)
                {
                    // the game is in the list 
                    int gameIndex = gameList.MatchingGames.FindIndex(mat => mat.Game.Id == preChangeGameId);
                    if (gameIndex >= 0)
                    {
                        // jump to the game
                        RandomGameSelection.DoRandomGame(gameList.ListSetStartIndex + gameIndex);
                        return;
                    }
                }

                // the game was not in the list so try the next game in the list 
                if (gameList?.MatchingGames?.Count() > preChangeGameIndex)
                {
                    RandomGameSelection.DoRandomGame(gameList.ListSetStartIndex + preChangeGameIndex);
                    return;
                }

                // there was no next game so try a previous game in the list 
                if (gameList?.MatchingGames?.Count() > preChangeGameIndex - 1)
                {
                    RandomGameSelection.DoRandomGame(gameList.ListSetStartIndex + preChangeGameIndex - 1);
                    return;
                }

                // there was no next or previous, try just the first game in the list 
                if (gameList?.MatchingGames?.Count() > 0)
                {
                    RandomGameSelection.DoRandomGame(gameList.ListSetStartIndex);
                    return;
                }
            }
            else
            {
                // the list was not there so pick any random game
                RandomGameSelection.DoRandomGame();
                return;
            }
        }

        public void RateCurrentGame(float changeAmount)
        {
            GameOperations.RateCurrentGame(changeAmount);
        }

        public void SaveRatingCurrentGame()
        {
            GameOperations.SaveRatingCurrentGame();
        }

        public bool DoEnter()
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnEnter();
        }

        public bool DoEscape()
        {
            UIState.IsPlayingGame = false;
            return EclipseStateContext.OnEscape();
        }

        private GameList currentGameList;
        public GameList CurrentGameList
        {
            get => currentGameList;
            set
            {
                if (currentGameList != value)
                {
                    currentGameList = value;
                    OnPropertyChanged();
                }
            }
        }

        private System.Windows.Thickness frontImageMargin;
        public System.Windows.Thickness FrontImageMargin
        {
            get => frontImageMargin;
            set
            {
                frontImageMargin = value;
                OnPropertyChanged();
            }
        }

        private System.Windows.Thickness selectedGameDetailsPadding;
        public System.Windows.Thickness SelectedGameDetailsPadding
        {
            get => selectedGameDetailsPadding;
            set
            {
                selectedGameDetailsPadding = value;
                OnPropertyChanged();
            }
        }

        private GameList nextGameList;
        public GameList NextGameList
        {
            get => nextGameList;
            set
            {
                if (nextGameList != value)
                {
                    nextGameList = value;
                    OnPropertyChanged();
                }
            }
        }

        public Uri VoiceRecognitionGif { get; } = ResourceImages.VoiceRecognitionGif;

        public Uri SettingsIconGrey { get; } = ResourceImages.SettingsIconGrey;

        public Uri SettingsIconWhite { get; } = ResourceImages.SettingsIconWhite;

        public Uri LaunchBoxLogo { get; } = ResourceImages.LaunchBoxLogo;

        private FeatureGameOption featureOption;
        public FeatureGameOption FeatureOption
        {
            get { return featureOption; }
            set
            {
                if (featureOption != value)
                {
                    featureOption = value;
                    OnPropertyChanged();
                }
                SetButtonImages();
            }
        }

        private void SetButtonImages()
        {
            if (featureOption == FeatureGameOption.PlayGame)
            {
                PlayButtonImage = ResourceImages.PlayButtonSelected;
                MoreInfoImage = ResourceImages.MoreInfoUnSelected;
            }
            else
            {
                PlayButtonImage = ResourceImages.PlayButtonUnSelected;
                MoreInfoImage = ResourceImages.MoreInfoSelected;
            }
        }

        private Uri playButtonImage;
        public Uri PlayButtonImage
        {
            get => playButtonImage;
            set
            {
                if (playButtonImage != value)
                {
                    playButtonImage = value;
                    OnPropertyChanged();
                }
            }
        }

        private Uri moreInfoImage;
        public Uri MoreInfoImage
        {
            get => moreInfoImage;
            set
            {
                if (moreInfoImage != value)
                {
                    moreInfoImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Star1 => 1;
        public int Star2 => 2;
        public int Star3 => 3;
        public int Star4 => 4;
        public int Star5 => 5;
        public float StarOffset00 => 0.0f;
        public float StarOffset01 => 0.1f;
        public float StarOffset05 => 0.5f;
        public float StarOffset06 => 0.6f;
        public float StarOffset10 => 1.0f;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}