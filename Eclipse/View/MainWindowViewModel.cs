using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using Eclipse.Models;
using System.Threading.Tasks;
using Eclipse.Helpers;
using System.Threading;
using Eclipse.State;
using Eclipse.State.GameDetailOptions;
using Eclipse.Service;
using System.Windows.Threading;

namespace Eclipse.View
{
    public delegate void AnimateGameChangeFunction();
    public delegate void IncrementLoadingProgressFunction();
    public delegate void StopVideoAndAnimations();

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Where the user is - which set, which list, which game - and everything that moves
        /// them. CurrentGameList and NextGameList below are a copy of what it decided, kept for
        /// the XAML to bind to; the navigator is the one that knows.
        /// </summary>
        public GameListNavigator Navigator { get; } = new GameListNavigator();

        /// <summary>
        /// The UI thread, for the one place that has to come back to it from somewhere else.
        ///
        /// WPF constructs this view model from the &lt;local:MainWindowViewModel/&gt; element in
        /// MainWindowView.xaml, so the constructor runs on the dispatcher thread and this is it.
        /// Voice recognition is the only part of Eclipse that starts on the UI thread and
        /// finishes on a thread pool one; it needs somewhere to marshal back to, and this is the
        /// only object in that call path guaranteed to have been built here.
        /// </summary>
        public Dispatcher UiDispatcher { get; } = Dispatcher.CurrentDispatcher;

        /// <summary>The options in the game detail overlay. Driven by GameDetailOptionsState.</summary>
        public GameDetailOptionList GameDetailOptions { get; } = new GameDetailOptionList();

        public GameCatalog gameCatalog;
        public IReadOnlyList<GameFiles> gameFilesBag;

        private bool isInitializing;
        private bool isPickingCategory;
        private bool isDisplayingFeature;
        private bool isDisplayingAttractMode;
        private VoiceSearchPhase voiceSearchPhase;
        private string heardPhrase;
        private bool isDisplayingResults;
        private bool isDisplayingMoreInfo;
        private bool isDisplayingError;
        private bool isDisplayingSearch;
        private bool isRatingGame;
        private bool isZoomingBox;
        private bool isPlayingGame;

        private double videoVolume;

        private string errorMessage;
        private string errorHint;

        public EclipseStateContext EclipseStateContext { get; set; }

        private Models.EclipseSettings eclipseSettings;

        public MainWindowViewModel()
        {
            IsInitializing = true;

            // The navigator decides; this repeats. Nothing else assigns CurrentGameList or
            // NextGameList, so the two cannot drift apart.
            Navigator.SelectionChanged += OnSelectionChanged;
            Navigator.NavigationFailed += OnNavigationFailed;

            FeatureOption = FeatureGameOption.PlayGame;

            InitializeEclipseSettings();

            EclipseStateContext = new EclipseStateContext(this);
        }

        public void InitializeEclipseSettings()
        {
            eclipseSettings = EclipseSettingsDataProvider.Instance?.EclipseSettings;
            VideoVolume = eclipseSettings?.DefaultVideoVolume ?? 0.5;
            ShowMatchPercent = eclipseSettings?.ShowMatchPercent ?? true;
            ShowReleaseYear = eclipseSettings?.ShowReleaseYear ?? true;
            ShowStarRating = eclipseSettings?.ShowStarRating ?? true;
            ShowPlayMode = eclipseSettings?.ShowPlayMode ?? true;
            ShowPlatformLogo = eclipseSettings?.ShowPlatformLogo ?? true;
            ShowOptionsIcon = eclipseSettings?.ShowOptionsIcon ?? true;

            double marginLeft = eclipseSettings?.BoxFrontMarginLeft ?? 2;
            double marginRight = eclipseSettings?.BoxFrontMarginRight ?? 2;
            double marginTop = eclipseSettings?.BoxFrontMarginTop ?? 2;
            double marginBottom = eclipseSettings?.BoxFrontMarginBottom ?? 2;

            double selectedGameDetailsPadding = eclipseSettings?.SelectedGameDetailsPadding ?? 0;

            FrontImageMargin = new System.Windows.Thickness(marginLeft, marginTop, marginRight, marginBottom);
            SelectedGameDetailsPadding = new System.Windows.Thickness(selectedGameDetailsPadding);
        }

        public bool ShowMatchPercent
        {
            get => eclipseSettings.ShowMatchPercent;
            set
            {
                eclipseSettings.ShowMatchPercent = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ShowMatchPercent"));
            }
        }

        public bool ShowPlatformLogo
        {
            get => eclipseSettings.ShowPlatformLogo;
            set
            {
                eclipseSettings.ShowPlatformLogo = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ShowPlatformLogo"));
            }
        }

        public bool ShowPlayMode
        {
            get => eclipseSettings.ShowPlayMode;
            set
            {
                eclipseSettings.ShowPlayMode = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ShowPlayMode"));
            }
        }

        public bool ShowReleaseYear
        {
            get => eclipseSettings.ShowReleaseYear;
            set
            {
                eclipseSettings.ShowReleaseYear = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ShowReleaseYear"));
            }
        }

        public bool ShowStarRating
        {
            get => eclipseSettings.ShowStarRating;
            set
            {
                eclipseSettings.ShowStarRating = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ShowStarRating"));
            }
        }

        public bool ShowOptionsIcon
        {
            get => eclipseSettings.ShowOptionsIcon;
            set
            {
                eclipseSettings.ShowOptionsIcon = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ShowOptionsIcon"));
            }
        }

        public bool IsPlayingGame
        {
            get => isPlayingGame;
            set
            {
                isPlayingGame = value;
                PropertyChanged(this, new PropertyChangedEventArgs("IsPlayingGame"));
            }
        }

        public double VideoVolume
        {
            get => videoVolume;
            set
            {
                if (videoVolume != value)
                {
                    videoVolume = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("VideoVolume"));
                }
            }
        }

        public bool IsInitializing
        {
            get => isInitializing;
            set
            {
                if (isInitializing != value)
                {
                    isInitializing = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsInitializing"));
                }
            }
        }

        public bool IsZoomingBox 
        {
            get => isZoomingBox;
            set
            {
                if (isZoomingBox != value)
                {
                    isZoomingBox = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsZoomingBox"));
                }
            }
        }

        public bool IsPickingCategory
        {
            get => isPickingCategory;
            set
            {
                if (isPickingCategory != value)
                {
                    isPickingCategory = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsPickingCategory"));
                }
            }
        }

        public bool IsDisplayingFeature
        {
            get => isDisplayingFeature;
            set
            {
                if (isDisplayingFeature != value)
                {
                    isDisplayingFeature = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingFeature"));
                }
            }
        }

        public bool IsDisplayingAttractMode
        {
            get => isDisplayingAttractMode;
            set
            {
                if (isDisplayingAttractMode != value)
                {
                    isDisplayingAttractMode = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingAttractMode"));
                }
            }
        }

        public VoiceSearchPhase VoiceSearchPhase
        {
            get => voiceSearchPhase;
            private set
            {
                if (voiceSearchPhase != value)
                {
                    voiceSearchPhase = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("VoiceSearchPhase"));
                }
            }
        }

        /// <summary>
        /// The recogniser's best guess so far, echoed under the listening indicator. Null when
        /// nothing has been heard yet. It changes as the user speaks and it may well be wrong -
        /// that is the point of showing it, because it is what the search will be run against.
        /// </summary>
        public string HeardPhrase
        {
            get => heardPhrase;
            set
            {
                if (heardPhrase != value)
                {
                    heardPhrase = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("HeardPhrase"));
                }
            }
        }

        /// <summary>
        /// Puts the screen into voice search.
        ///
        /// The screen the search was started from has to get out of the way, and nothing used to
        /// do that: starting from the detail overlay left Play / Favourite / More like this
        /// sitting behind the listening indicator for the whole session, and starting from the
        /// category picker left the picker there until the search finished. Both were only
        /// cleared afterwards, on the way back into browsing.
        /// </summary>
        public void EnterVoiceSearch()
        {
            IsDisplayingFeature = false;
            IsDisplayingMoreInfo = false;
            IsPickingCategory = false;

            HeardPhrase = null;
            VoiceSearchPhase = VoiceSearchPhase.Listening;
        }

        /// <summary>
        /// Takes the screen back out of voice search. Every way out goes through here - finished,
        /// cancelled, heard nothing, matched nothing, failed - so none of them can half-do it.
        /// </summary>
        public void LeaveVoiceSearch()
        {
            VoiceSearchPhase = VoiceSearchPhase.Inactive;
            HeardPhrase = null;
        }

        public bool IsDisplayingResults
        {
            get => isDisplayingResults;
            set
            {
                if (isDisplayingResults != value)
                {
                    isDisplayingResults = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingResults"));
                }
            }
        }

        public bool IsDisplayingError
        {
            get => isDisplayingError;
            set
            {
                if (isDisplayingError != value)
                {
                    isDisplayingError = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingError"));
                }
            }
        }

        public bool IsDisplayingSearch
        {
            get => isDisplayingSearch;
            set
            {
                if (isDisplayingSearch != value)
                {
                    isDisplayingSearch = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingSearch"));
                }
            }
        }

        public bool IsDisplayingMoreInfo
        {
            get => isDisplayingMoreInfo;
            set
            {
                if (isDisplayingMoreInfo != value)
                {
                    isDisplayingMoreInfo = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingMoreInfo"));
                }
            }
        }

        public bool IsRatingGame
        {
            get => isRatingGame;
            set
            {
                if (isRatingGame != value)
                {
                    isRatingGame = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsRatingGame"));
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
                    PropertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
                }
            }
        }

        /// <summary>
        /// What the user can do about the message above, shown underneath it. Null where there is
        /// nothing to do - a message with no way forward should not pretend to offer one.
        /// </summary>
        public string ErrorHint
        {
            get => errorHint;
            set
            {
                if (errorHint != value)
                {
                    errorHint = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ErrorHint"));
                }
            }
        }

        public AnimateGameChangeFunction GameChangeFunction { get; set; }
        public StopVideoAndAnimations StopVideoAndAnimationsFunction { get; set; }

        private OptionList optionList;
        public OptionList OptionList
        {
            get => optionList;
            set
            {
                if (optionList != value)
                {
                    optionList = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OptionList"));
                }
            }
        }

        public async void SetupFiles(object sender, DoWorkEventArgs e)
        {
            int? GameFilesCount = gameFilesBag?.Count;
            int processedCount = 0;

            BrowsePerformanceMonitor.Instance.PumpStarted(GameFilesCount ?? 0);

            await Task.Run(async () =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                while (await SetupNextGameFiles())
                {
                    BrowsePerformanceMonitor.Instance.PumpIterationCompleted();

                    // just to be safe and avoid an infinite loop
                    // check how many times we've been through the loop and stop after we have
                    // processed enough to go through all game files
                    processedCount++;
                    if (processedCount > GameFilesCount)
                    {
                        break;
                    }
                }
            });

            BrowsePerformanceMonitor.Instance.PumpFinished(
                gameFilesBag?.Count(gameFiles => gameFiles.IsSetup) ?? 0,
                GameFilesCount ?? 0);
        }

        // The pump's "has this one been done already" test, counted. Every call site below used
        // to be a bare lambda; routing them through here is what makes the quadratic re-scan
        // visible as a number rather than as a suspicion.
        private static bool NeedsSetup(GameFiles gameFiles)
        {
            BrowsePerformanceMonitor.Instance.PumpPredicateEvaluated();
            return !gameFiles.IsSetup;
        }

        private async Task<bool> SetupNextGameFiles()
        {
            bool moreGameFiles = false;

            try
            {
                await Task.Run(async () =>
                {
                    // setup a game in the current list 
                    if (CurrentGameList != null && CurrentGameList.MatchingGames != null)
                    {
                        IEnumerable<GameMatch> currentListQuery = CurrentGameList.MatchingGames.Where(g => NeedsSetup(g.GameFiles));
                        if (currentListQuery.Any())
                        {
                            GameMatch gameMatchCurrentList = currentListQuery.FirstOrDefault();
                            if (gameMatchCurrentList?.GameFiles != null)
                            {
                                moreGameFiles = true;
                                await gameMatchCurrentList.GameFiles.SetupFiles();

                                if (gameMatchCurrentList?.GameFiles?.Game?.Id == currentGameList?.SelectedGame?.Game?.Id)
                                {
                                    CallGameChangeFunction();
                                }
                            }
                        }
                    }

                    // setup a game in the next list
                    if (NextGameList != null && NextGameList.MatchingGames != null)
                    {
                        IEnumerable<GameMatch> nextListQuery = NextGameList.MatchingGames.Where(g => NeedsSetup(g.GameFiles));
                        if (nextListQuery.Any())
                        {
                            GameMatch gameMatchNextList = nextListQuery.FirstOrDefault();
                            if (gameMatchNextList?.GameFiles != null)
                            {
                                moreGameFiles = true;
                                await gameMatchNextList.GameFiles.SetupFiles();
                            }
                        }
                    }

                    // setup any game that still needs to be setup
                    if (gameFilesBag != null)
                    {
                        IEnumerable<GameFiles> anyGameQuery = gameFilesBag.Where(gf => NeedsSetup(gf));
                        if (anyGameQuery.Any())
                        {
                            GameFiles anyGameFiles = anyGameQuery.FirstOrDefault();
                            if (anyGameFiles != null)
                            {
                                moreGameFiles = true;
                                await anyGameFiles.SetupFiles();
                            }
                        }
                    }
                });

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "SetupNextGameFiles");
            }

            return moreGameFiles;
        }

        public void CreateGameLists()
        {
            // Read the custom list definitions once. This used to be reloaded and
            // deserialised from disk inside every category's build - eight file reads per
            // rebuild, and a rebuild happens on every favourite, rating change and game launch.
            IReadOnlyList<CustomListDefinition> customListDefinitions =
                new CustomListDefinitionDataProvider().GetAllCustomListDefinitions().ToList();

            // Setting the catalog up is what populates the playlist service, and until now the
            // playlist dictionary was read part-way through building the platform set - always
            // after the catalog had been touched. Keep that order: read from a cold service and
            // the dictionary builds itself, then gets rebuilt underneath us during the catalog's
            // own setup, leaving this holding the previous instance. Same contents either way.
            _ = gameCatalog.Games;

            GameListBuilder gameListBuilder = new GameListBuilder(gameCatalog,
                                                                  customListDefinitions,
                                                                  PlaylistGameService.Instance.Playlists);

            // Replace each set rather than clearing the collection: the voice search and
            // more-like-this sets are built elsewhere from results, and a rebuild triggered by
            // favouriting a game has no business discarding them.
            foreach (GameListSet gameListSet in gameListBuilder.BuildAll())
            {
                Navigator.InstallSet(gameListSet);
            }
        }

        public void DoMoreLikeCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.SelectedGame;
            if (currentGame != null)
            {
                List<GameList> moreLikeThisResults = GameListBuilder.BuildMoreLikeThis(currentGame, Navigator.Sets);

                Navigator.InstallSet(new GameListSet
                {
                    ListCategoryType = ListCategoryType.MoreLikeThis,
                    GameLists = moreLikeThisResults
                });

                Navigator.ShowCategory(ListCategoryType.MoreLikeThis);
                IsDisplayingResults = true;
                IsDisplayingFeature = false;
                IsDisplayingMoreInfo = false;
                CallGameChangeFunction();
            }
        }

        // Each game is equally likely, so this only needs a source of numbers - the weighting
        // that random game does lives in the navigator with the lists it weights by.
        private static readonly Random random = new Random();

        /// <summary>
        /// Picks the next game for the screen saver and returns it, or null if the library is
        /// empty. The slideshow needs the game itself so it can hydrate and decode its
        /// artwork before showing the slide.
        /// </summary>
        public GameMatch NextAttractModeGame()
        {
            // Each game is equally likely. This used to pick from the flat bag, where a game
            // appeared once per category value and once per voice phrase - so games with
            // long titles or rich metadata turned up several times more often than others.
            IReadOnlyList<GameMatch> games = gameCatalog.Games;
            if (games.Count == 0)
            {
                return null;
            }

            return games[random.Next(games.Count)];
        }
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            CurrentGameList = Navigator.CurrentList;
            NextGameList = Navigator.NextList;

            // These two lists are the only ones on screen, so they are the only ones whose
            // artwork is worth decoding ahead. Moving between lists brings a row that has never
            // been warmed, which is the one case where the decoder starts from nothing.
            CurrentGameList?.WarmRowImages();
            NextGameList?.WarmRowImages();

            CallGameChangeFunction();
        }

        private void OnNavigationFailed(object sender, EventArgs e)
        {
            DisplayingErrorState displayingErrorState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
            displayingErrorState.ErrorMessage = "A problem occurred trying to refresh the list of games";
            EclipseStateContext.TransitionToState(displayingErrorState);
        }

        // Which lists have changed under the user since their position was noted. Favouriting,
        // rating and launching a game all change what the lists contain; the rebuild is deferred
        // until the detail overlay closes rather than happening on each change.
        private bool gameListsChanged;

        private void SaveStateForGameListChange()
        {
            gameListsChanged = true;
            Navigator.RememberPosition();
        }

        public void CheckResetGameLists()
        {
            if (!gameListsChanged)
            {
                return;
            }

            gameListsChanged = false;

            CreateGameLists();
            Navigator.RestorePosition();
        }

        public void CallGameChangeFunction()
        {
            GameChangeFunction?.Invoke();
        }

        public void CallStopVideoAndAnimationsFunction()
        {
            StopVideoAndAnimationsFunction?.Invoke();
        }
        public void AdjustVideoVolume(double increment)
        {
            if (VideoVolume + increment > 1)
            {
                VideoVolume = 1;
            }
            else if (VideoVolume + increment < 0)
            {
                VideoVolume = 0;
            }
            else
            {
                VideoVolume += increment;
            }
        }

        /// <summary>
        /// Puts a set of lists in place, replacing whatever set of the same category was there.
        ///
        /// Three places used to write this out longhand - building the category sets, voice
        /// search, and more like this - each removing by category and adding, in three different
        /// files. Replacing rather than clearing matters: a rebuild triggered by favouriting a
        /// game must not throw away the voice search results or the more-like-this set.
        /// </summary>

        public bool DoUp(bool held)
        {
            IsPlayingGame = false;
            return EclipseStateContext.OnUp(held);
        }

        public bool DoDown(bool held)
        {
            IsPlayingGame = false;
            return EclipseStateContext.OnDown(held);
        }

        public bool DoLeft(bool held)
        {
            IsPlayingGame = false;
            return EclipseStateContext.OnLeft(held);
        }

        public bool DoRight(bool held)
        {
            IsPlayingGame = false;
            return EclipseStateContext.OnRight(held);
        }

        public bool DoPageUp()
        {
            IsPlayingGame = false;
            return EclipseStateContext.OnPageUp();
        }

        public bool DoPageDown()
        {
            IsPlayingGame = false;
            return EclipseStateContext.OnPageDown();
        }


        // start the current game
        public void PlayCurrentGame()
        {
            // get a handle on the current game
            IGame currentGame = CurrentGameList?.SelectedGame?.Game;
            IAdditionalApplication additionalApplication =
                CurrentGameList?.SelectedGame?.GameFiles?.GameVersionList?.SelectedGameVersion?.AdditionalApplication;

            if (currentGame != null)
            {
                currentGame.LastPlayedDate = DateTime.Now;

                // reset the lists so the updated history reflects - first save the current game details then reload the lists
                SaveStateForGameListChange();
                CheckResetGameLists();

                // stop everything in the UI
                CallStopVideoAndAnimationsFunction();

                IsPlayingGame = true;

                // launch the game 
                PluginHelper.BigBoxMainViewModel.PlayGame(currentGame, additionalApplication, null, null);
            }
            return;
        }

        // mark current game as a favorite
        public void FavoriteCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.SelectedGame;
            if (currentGame != null)
            {
                currentGame.Favorite = !currentGame.Favorite;

                PluginHelper.DataManager.Save(false);

                // every list holds the same object for a given game, so the assignment above
                // is already visible everywhere it appears - there is nothing to propagate

                // save state so we can get back to the current game
                SaveStateForGameListChange();
            }
        }


        public void RateCurrentGame(float changeAmount)
        {
            GameMatch currentGame = CurrentGameList?.SelectedGame;
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

        // The rating being edited is written straight into the game as the user moves it, so
        // the stars track live. This remembers what it was on the way in, which is what lets
        // Escape put it back - and it doubles as the marker for whether anything has actually
        // changed, so leaving without touching the rating does not write to the LaunchBox data.
        private float? ratingBeforeEditing;

        /// <summary>Call when the rating editor opens, so a later cancel has something to restore.</summary>
        public void BeginRatingCurrentGame()
        {
            ratingBeforeEditing = CurrentGameList?.SelectedGame?.UserRating;
        }

        /// <summary>Put the rating back to what it was when the editor opened.</summary>
        public void CancelRatingCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.SelectedGame;

            if (currentGame != null && ratingBeforeEditing.HasValue)
            {
                currentGame.UserRating = ratingBeforeEditing.Value;
            }

            ratingBeforeEditing = null;
        }

        public void SaveRatingCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.SelectedGame;
            if (currentGame == null)
            {
                return;
            }

            // Nothing moved, so there is nothing to write. Enter can be pressed repeatedly in
            // the rating editor and each press used to run a full LaunchBox save.
            if (ratingBeforeEditing.HasValue && ratingBeforeEditing.Value == currentGame.UserRating)
            {
                return;
            }

            // save the rating change to the launchbox data
            PluginHelper.DataManager.Save(false);

            // committed - a cancel after this point goes back to the value just saved
            ratingBeforeEditing = currentGame.UserRating;
        }

        public bool DoEnter()
        {
            IsPlayingGame = false;
            return EclipseStateContext.OnEnter();
        }

        public bool DoEscape()
        {
            IsPlayingGame = false;
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
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentGameList"));
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
                PropertyChanged(this, new PropertyChangedEventArgs("FrontImageMargin"));
            }
        }

        private System.Windows.Thickness selectedGameDetailsPadding;
        public System.Windows.Thickness SelectedGameDetailsPadding
        {
            get => selectedGameDetailsPadding;
            set
            {
                selectedGameDetailsPadding = value;
                PropertyChanged(this, new PropertyChangedEventArgs("SelectedGameDetailsPadding"));
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
                    PropertyChanged(this, new PropertyChangedEventArgs("NextGameList"));
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
                    PropertyChanged(this, new PropertyChangedEventArgs("FeatureOption"));
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
                    PropertyChanged(this, new PropertyChangedEventArgs("PlayButtonImage"));
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
                    PropertyChanged(this, new PropertyChangedEventArgs("MoreInfoImage"));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}