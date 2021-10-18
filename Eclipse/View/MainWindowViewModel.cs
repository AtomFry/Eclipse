using Eclipse.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using System.Data;
using Eclipse.Models;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Media.TextFormatting;
using System.Security.RightsManagement;
using System.Windows.Threading;
using Eclipse.Helpers;

namespace Eclipse.View
{
    public delegate void FeatureChangeFunction();
    public delegate void AnimateGameChangeFunction();
    
    public delegate void IncrementLoadingProgressFunction();
    public delegate void StopVideoAndAnimations();
    public delegate void UpdateRatingImage();

    class MainWindowViewModel : INotifyPropertyChanged
    {
        private List<Option<ListCategoryType>> listCategories;
        private ListCycle<GameList> listCycle;
        public List<GameListSet> GameListSets { get; set; }

        public SpeechRecognizer SpeechRecognizer { get; set; }

        private ConcurrentBag<GameMatch> GameBag = new ConcurrentBag<GameMatch>();
        private ConcurrentBag<GameFiles> GameFilesBag = new ConcurrentBag<GameFiles>();

        // store platform bezels and default bezels in dictionary for easy lookup
        // this is probably terrible design but the problem is I won't know the video orientation in the GameMatch, I get that once the MediaElement is loaded over in the view
        // TODO: add game title (or id) here to take care of all game bezels too - currently game specific bezels are loaded up in the GameMatch
        // Key: <BezelType, BezelOrientation, Platform> 
        private Dictionary<Tuple<BezelType, BezelOrientation, string>, Uri> BezelDictionary;

        public FeatureChangeFunction FeatureChangeFunction { get; set; }
        public AnimateGameChangeFunction GameChangeFunction { get; set; }
        public StopVideoAndAnimations StopVideoAndAnimationsFunction { get; set; }
        public UpdateRatingImage UpdateRatingImageFunction { get; set; }

        private GameDetailOption gameDetailOption;
        public GameDetailOption GameDetailOption
        {
            get { return gameDetailOption; }
            set
            {
                {
                    gameDetailOption = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("GameDetailOption"));
                }
            }
        }


        private List<IGame> allGames;
        public List<IGame> AllGames
        {
            get { return allGames; }
            set
            {
                if (allGames != value)
                {
                    allGames = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("AllGames"));
                }
            }
        }

        private bool isInitializing;
        public bool IsInitializing
        {
            get { return isInitializing; }
            set
            {
                if (isInitializing != value)
                {
                    isInitializing = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsInitializing"));
                }
            }
        }

        private bool isPickingCategory;
        public bool IsPickingCategory
        {
            get { return isPickingCategory; }
            set
            {
                if (isPickingCategory != value)
                {
                    isPickingCategory = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsPickingCategory"));
                }
            }
        }

        private bool isDisplayingFeature;
        public bool IsDisplayingFeature
        {
            get { return isDisplayingFeature; }
            set
            {
                if (isDisplayingFeature != value)
                {
                    isDisplayingFeature = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingFeature"));
                }
            }
        }

        private bool isDisplayingAttractMode;
        public bool IsDisplayingAttractMode
        {
            get { return isDisplayingAttractMode; }
            set
            {
                if (isDisplayingAttractMode != value)
                {
                    isDisplayingAttractMode = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingAttractMode"));
                }
            }
        }

        private bool isRecognizing;
        public bool IsRecognizing
        {
            get { return isRecognizing; }
            set
            {
                if (isRecognizing != value)
                {
                    isRecognizing = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsRecognizing"));
                }
            }
        }

        private bool isDisplayingResults;
        public bool IsDisplayingResults
        {
            get { return isDisplayingResults; }
            set
            {
                if (isDisplayingResults != value)
                {
                    isDisplayingResults = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingResults"));
                }
            }
        }

        private bool isDisplayingError;
        public bool IsDisplayingError
        {
            get { return isDisplayingError; }
            set
            {
                if (isDisplayingError != value)
                {
                    isDisplayingError = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingError"));
                }
            }
        }

        private bool isDisplayingMoreInfo;
        public bool IsDisplayingMoreInfo
        {
            get { return isDisplayingMoreInfo; }
            set
            {
                if (isDisplayingMoreInfo != value)
                {
                    isDisplayingMoreInfo = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingMoreInfo"));
                }
            }
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                if (errorMessage != value)
                {
                    errorMessage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
                }
            }
        }


        private int totalProgressStepsCount;
        public int TotalProgressStepsCount
        {
            get { return totalProgressStepsCount; }
            set
            {
                if (totalProgressStepsCount != value)
                {
                    totalProgressStepsCount = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("TotalProgressStepsCount"));
                }
            }
        }

        private string loadingMessage;
        public string LoadingMessage
        {
            get { return loadingMessage; }
            set
            {
                if (loadingMessage != value)
                {
                    loadingMessage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("LoadingMessage"));
                }
            }
        }
        private int completedProgressStepsCount;
        public int CompletedProgressStepsCount
        {
            get { return completedProgressStepsCount; }
            set
            {
                if (completedProgressStepsCount != value)
                {
                    completedProgressStepsCount = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("CompletedProgressStepsCount"));
                }
            }
        }

        private List<Option> categoryList;
        public List<Option> CategoryList
        {
            get { return categoryList; }
            set
            {
                if (categoryList != value)
                {
                    categoryList = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("CategoryList"));
                }
            }
        }

        private bool isRatingGame;
        public bool IsRatingGame
        {
            get { return isRatingGame; }
            set
            {
                if(isRatingGame != value)
                {
                    isRatingGame = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsRatingGame"));
                }
            }
        }


        private GameListSet currentGameListSet;
        public GameListSet CurrentGameListSet
        {
            get { return currentGameListSet; }
            set
            {
                if (currentGameListSet != value)
                {
                    currentGameListSet = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentGameListSet"));
                }
            }
        }

        private OptionList optionList;
        public OptionList OptionList
        {
            get { return optionList; }
            set
            {
                if (optionList != value)
                {
                    optionList = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OptionList"));
                }
            }
        }

        private void SetupCategoryList()
        {
            listCategories = new List<Option<ListCategoryType>>();
            listCategories.Add(new Option<ListCategoryType> { Name = "Platform", EnumOption = ListCategoryType.Platform, SortOrder = 1, ShortDescription = "Platform", LongDescription = "Platform" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Genre", EnumOption = ListCategoryType.Genre, SortOrder = 2, ShortDescription = "Genre", LongDescription = "Genre" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Series", EnumOption = ListCategoryType.Series, SortOrder = 3, ShortDescription = "Series", LongDescription = "Series" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Playlist", EnumOption = ListCategoryType.Playlist, SortOrder = 4, ShortDescription = "Playlist", LongDescription = "Playlist" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Play mode", EnumOption = ListCategoryType.PlayMode, SortOrder = 5, ShortDescription = "Mode", LongDescription = "Play mode" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Developer", EnumOption = ListCategoryType.Developer, SortOrder = 6, ShortDescription = "Dev", LongDescription = "Developer" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Publisher", EnumOption = ListCategoryType.Publisher, SortOrder = 7, ShortDescription = "Pub", LongDescription = "Publisher" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Year", EnumOption = ListCategoryType.ReleaseYear, SortOrder = 8, ShortDescription = "Year", LongDescription = "Release year" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Random", EnumOption = ListCategoryType.RandomGame, SortOrder = 9, ShortDescription = "Random", LongDescription = "Random game" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Voice search", EnumOption = ListCategoryType.VoiceSearch, SortOrder = 10, ShortDescription = "Voice", LongDescription = "Voice search" });
            OptionList = new OptionList(listCategories);
        }

        // get platform bezels if game bezel is not there
        private void GetPlatformBezels()
        {
            // platform bezel path 
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Horizontal.png
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Vertical.png
            List<IPlatform> platforms = new List<IPlatform>(PluginHelper.DataManager.GetAllPlatforms());
            foreach (var platform in platforms)
            {
                Tuple<BezelType, BezelOrientation, string> platformHorizontalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.PlatformDefault, BezelOrientation.Horizontal, platform.Name);
                Tuple<BezelType, BezelOrientation, string> platformVerticalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.PlatformDefault, BezelOrientation.Vertical, platform.Name);

                string platformHorizontalBezelPath = Path.Combine(DirectoryInfoHelper.Instance.BezelFolder, platform.Name, "Horizontal.png");
                string platformVerticalBezelPath = Path.Combine(DirectoryInfoHelper.Instance.BezelFolder, platform.Name, "Vertical.png");

                if (File.Exists(platformHorizontalBezelPath)) BezelDictionary.Add(platformHorizontalBezelKey, new Uri(platformHorizontalBezelPath));
                if (File.Exists(platformVerticalBezelPath)) BezelDictionary.Add(platformVerticalBezelKey, new Uri(platformVerticalBezelPath));
            }
        }

        // get default bezels if game bezel and platform bezel is not there
        private void GetDefaultBezels()
        {
            // default bezel path 
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\DEFAULT\Horizontal.png
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\DEFAULT\Vertical.png
            Tuple<BezelType, BezelOrientation, string> defaultHorizontalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.Default, BezelOrientation.Horizontal, string.Empty);
            Tuple<BezelType, BezelOrientation, string> defaultVerticalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.Default, BezelOrientation.Vertical, string.Empty);

            string defaultHorizontalBezelPath = Path.Combine(DirectoryInfoHelper.Instance.BezelFolder, "Default", "Horizontal.png");
            string defaultVerticalBezelPath = Path.Combine(DirectoryInfoHelper.Instance.BezelFolder, "Default", "Vertical.png");

            if (File.Exists(defaultHorizontalBezelPath)) BezelDictionary.Add(defaultHorizontalBezelKey, new Uri(defaultHorizontalBezelPath));
            if (File.Exists(defaultVerticalBezelPath)) BezelDictionary.Add(defaultVerticalBezelKey, new Uri(defaultVerticalBezelPath));
        }

        private void GetGamesByListCategoryType(ListCategoryType listCategoryType, bool includeFavorites = false, bool includeHistory = false)
        {
            List<GameList> listOfGameList = new List<GameList>();

            // remove any prior set of this type and then add these results to the set list category
            GameListSets.RemoveAll(set => set.ListCategoryType == listCategoryType);

            if (includeFavorites)
            {
                var favoriteGames = from gameMatch in GameBag
                                    where gameMatch.CategoryType == ListCategoryType.Favorites
                                    && gameMatch.Game.Favorite == true
                                    group gameMatch by gameMatch.CategoryValue into favoritesGroup
                                    select favoritesGroup;

                foreach (var favoritesGroup in favoriteGames)
                {
                    listOfGameList.Add(new GameList(favoritesGroup.Key, favoritesGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList(), true));
                }
            }

            if(includeHistory)
            {
                var historyGames = from gameMatch in GameBag
                                   where gameMatch.CategoryType == ListCategoryType.History
                                   && gameMatch.Game.LastPlayedDate != null
                                   group gameMatch by gameMatch.CategoryValue into historyGroup
                                   select historyGroup;

                foreach(var historyGroup in historyGames)
                {
                    listOfGameList.Add(new GameList(historyGroup.Key, historyGroup.OrderByDescending(game => game.Game.LastPlayedDate).ToList(), false, true));
                }
            }

            var gameQuery = from gameMatch in GameBag
                            where gameMatch.CategoryType == listCategoryType
                            group gameMatch by gameMatch.CategoryValue into gameGroup
                            select gameGroup;
           
            foreach (var gameGroup in gameQuery)
            {
                listOfGameList.Add(new GameList(gameGroup.Key, gameGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList()));
            }

            GameListSets.Add(new GameListSet
            {
                GameLists = listOfGameList.OrderByDescending(list=>list.IsFavorites)
                                            .ThenByDescending(list=>list.IsHistory)
                                            .ThenBy(list => list.ListDescription).ToList(),
                ListCategoryType = listCategoryType
            });
        }

        public MainWindowViewModel()
        {
            IsInitializing = true;
            FeatureOption = FeatureGameOption.PlayGame;

            BezelDictionary = new Dictionary<Tuple<BezelType, BezelOrientation, string>, Uri>();

            // get all the games from the launchbox install
            AllGames = DataService.GetGames();
        }

        public Uri GetDefaultBezel(BezelType bezelType, BezelOrientation bezelOrientation, string platformName)
        {
            Uri bezelUri = GetBezel(bezelType, bezelOrientation, platformName);
            if (bezelUri != null)
            {
                return bezelUri;
            }

            // try the default bezel
            if (bezelType != BezelType.Default)
            {
                bezelUri = GetBezel(BezelType.Default, bezelOrientation, string.Empty);
            }

            return bezelUri;
        }

        private Uri GetBezel(BezelType bezelType, BezelOrientation bezelOrientation, string platformName)
        {
            Uri bezelUri;

            Tuple<BezelType, BezelOrientation, string> bezelKey = new Tuple<BezelType, BezelOrientation, string>(bezelType, bezelOrientation, platformName);
            BezelDictionary.TryGetValue(bezelKey, out bezelUri);

            return bezelUri;
        }

        public void InitializeData()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Initialization_LoadData;
            worker.RunWorkerAsync();
        }

        private static List<PlaylistGame> GetPlaylistGames()
        {
            IPlaylist[] allPlaylists = PluginHelper.DataManager.GetAllPlaylists();
            List<PlaylistGame> playlistGames = new List<PlaylistGame>();
            foreach(IPlaylist playlist in allPlaylists)
            {
                if(playlist.HasGames(false, false))
                {
                    IGame[] games = playlist.GetAllGames(true);
                    foreach(IGame game in games)
                    {
                        playlistGames.Add(new PlaylistGame() { GameId = game.Id, Playlist = playlist.SortTitleOrTitle });
                    }
                }
            }

            return playlistGames;
        }

        private int GamesToProcessCount = 0;
        private int GamesProcessedCount = 0;
        private int ImagesToScaleCount = 0;
        private int ImagesScaledCount = 0;

        private DateTime ImageScalingStartTime;
        private void updateLoadingScaledImagesMessage()
        {
            if (ImagesScaledCount % 10 != 0) return;

            TimeSpan elapsedTime = DateTime.Now - ImageScalingStartTime;
            double elapsedSeconds = elapsedTime.TotalSeconds;

            if (elapsedSeconds == 0) elapsedSeconds = 1;

            double scaleRate = ImagesScaledCount / elapsedSeconds;

            if (scaleRate == 0) scaleRate = 1;

            double remainingSeconds = (ImagesToScaleCount - ImagesScaledCount) / scaleRate;

            TimeSpan remainingTimeSpan = TimeSpan.FromSeconds(remainingSeconds);

            LoadingMessage = string.Format("Updating image cache ({3}/{4}) {0:D2}:{1:D2}:{2:D2}", remainingTimeSpan.Hours, remainingTimeSpan.Minutes, remainingTimeSpan.Seconds, ImagesScaledCount, ImagesToScaleCount);
        }

        private DateTime ListCreationStartTime;
        private void updateRemainingLoadingMessage()
        {
            if (GamesProcessedCount % 10 != 0) return;

            TimeSpan elapsedTime = DateTime.Now - ListCreationStartTime;
            double elapsedSeconds = elapsedTime.TotalSeconds;

            if (elapsedSeconds == 0) elapsedSeconds = 1;

            double gameProcessRate = GamesProcessedCount / elapsedSeconds;

            if (gameProcessRate == 0) gameProcessRate = 1;

            double remainingSeconds = (GamesToProcessCount - GamesProcessedCount) / gameProcessRate;

            TimeSpan remainingTimeSpan = TimeSpan.FromSeconds(remainingSeconds);

            LoadingMessage = string.Format("Creating game lists ({3}/{4}) {0:D2}:{1:D2}:{2:D2}", remainingTimeSpan.Hours, remainingTimeSpan.Minutes, remainingTimeSpan.Seconds, GamesProcessedCount, GamesToProcessCount);
        }

        void Initialization_LoadData(object sender, DoWorkEventArgs e)
        {
            try
            {
                // create folders that are required by the plugin
                DirectoryInfoHelper.CreateFolders();

                // gets games by playlist so they can be used while processing games below 
                List<PlaylistGame> playlistGames = GetPlaylistGames();

                // get platform bezels
                GetPlatformBezels();
                GetDefaultBezels();

                // get the total count of games for the progress bar
                TotalProgressStepsCount = AllGames?.Count ?? 0;
                GamesToProcessCount = AllGames?.Count ?? 0;

                // prescale box front images - doing this here so it's easier to update the progress bar
                // get list of image files in launchbox folders that are missing from the plug-in folders
                List<FileInfo> gameFrontFilesToProcess = ImageScaler.GetMissingGameFrontImageFiles();
                List<FileInfo> platformLogosToProcess = ImageScaler.GetMissingPlatformClearLogoFiles();
                List<FileInfo> gameClearLogosToProcess = ImageScaler.GetMissingGameClearLogoFiles();
                bool scaleDefaultBoxFrontImage = !ImageScaler.DefaultBoxFrontExists();

                // get the desired height of pre-scaled box images based on the monitor's resolution
                // only need this if we have anything to process
                int desiredHeight = 0;
                if ((gameFrontFilesToProcess.Count > 0) || (platformLogosToProcess.Count > 0) || (gameClearLogosToProcess.Count > 0) || scaleDefaultBoxFrontImage)
                {
                    LoadingMessage = $"PRESCALING IMAGES";
                    desiredHeight = ImageScaler.GetDesiredHeight();
                }

                ImageScalingStartTime = DateTime.Now;


                // add the count of missing files for the loading progress bar
                TotalProgressStepsCount += (gameFrontFilesToProcess?.Count ?? 0);
                TotalProgressStepsCount += (platformLogosToProcess?.Count ?? 0);
                TotalProgressStepsCount += (gameClearLogosToProcess?.Count ?? 0);

                ImagesToScaleCount += (gameFrontFilesToProcess?.Count ?? 0);
                ImagesToScaleCount += (platformLogosToProcess?.Count ?? 0);
                ImagesToScaleCount += (gameClearLogosToProcess?.Count ?? 0);


                CompletedProgressStepsCount = 0;

                // scale the game front images
                foreach (FileInfo fileInfo in gameFrontFilesToProcess)
                {
                    ImagesScaledCount += 1;
                    CompletedProgressStepsCount += 1;
                    ImageScaler.ScaleImage(fileInfo, desiredHeight);
                    updateLoadingScaledImagesMessage();
                }

                // scale the default box front image
                if (scaleDefaultBoxFrontImage)
                {
                    ImagesScaledCount += 1;
                    CompletedProgressStepsCount += 1;
                    ImageScaler.ScaleDefaultBoxFront(desiredHeight);
                    updateLoadingScaledImagesMessage();
                }

                // crop platform clear logos 
                foreach (FileInfo fileInfo in platformLogosToProcess)
                {
                    ImagesScaledCount += 1;
                    CompletedProgressStepsCount += 1;
                    ImageScaler.CropImage(fileInfo);
                    updateLoadingScaledImagesMessage();
                }

                // crop game clear logos 
                foreach (FileInfo fileInfo in gameClearLogosToProcess)
                {
                    ImagesScaledCount += 1;
                    CompletedProgressStepsCount += 1;
                    ImageScaler.CropImage(fileInfo);
                    updateLoadingScaledImagesMessage();
                }

                ListCreationStartTime = DateTime.Now;

                Parallel.ForEach(AllGames, (game) =>
                {
                    GamesProcessedCount += 1;
                    CompletedProgressStepsCount += 1;
                    updateRemainingLoadingMessage();

                    GameFiles gameFiles = new GameFiles(game);
                    gameFiles.SetupFiles();
                    GameFilesBag.Add(gameFiles);

                    GameMatch gameMatch = new GameMatch(game, gameFiles);

                    if (game.Favorite)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Favorites, ListCategoryType.Favorites.ToString()));
                    }

                    if(game.LastPlayedDate != null)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.History, ListCategoryType.History.ToString()));
                    }

                    // create a dictionary of platform game matches
                    GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Platform, game.Platform));

                    // create a dictionary of release year game matches
                    GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.ReleaseYear, gameMatch.ReleaseYear));

                    // create a dictionary of play modes and game matches
                    foreach (string playMode in game.PlayModes)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.PlayMode, playMode));
                    }

                    // create a dictionary of Genres and game matches
                    foreach (string genre in game.Genres)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Genre, genre));
                    }

                    // create a dictionary of publishers and game matches
                    foreach (string publisher in game.Publishers)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Publisher, publisher));
                    }

                    // create a dictionary of developers and game matches
                    foreach (string developer in game.Developers)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Developer, developer));
                    }

                    // create a dictionary of series and game matches
                    foreach (string series in game.SeriesValues)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Series, series));
                    }

                    // create a dictionary of playlist and game matches 
                    var playlistGameQuery = from playlistGame in playlistGames where playlistGame.GameId == game.Id select playlistGame;
                    foreach(PlaylistGame playlistGame in playlistGameQuery)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Playlist, playlistGame.Playlist));
                    }

                    // create voice recognition grammar library for the game
                    GameTitleGrammarBuilder gameTitleGrammarBuilder = new GameTitleGrammarBuilder(game);
                    foreach(GameTitleGrammar gameTitleGrammar in gameTitleGrammarBuilder.gameTitleGrammars)
                    {
                        if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Title))
                        {
                            GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.Title, TitleMatchType.FullTitleMatch, gameTitleGrammar.Title));
                        }

                        if (!string.IsNullOrWhiteSpace(gameTitleGrammar.MainTitle))
                        {
                            GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.MainTitle, TitleMatchType.MainTitleMatch, gameTitleGrammar.Title));
                        }

                        if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Subtitle))
                        {
                            GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.Subtitle, TitleMatchType.SubtitleMatch, gameTitleGrammar.Title));
                        }

                        for (int i = 0; i < gameTitleGrammar.TitleWords.Count; i++)
                        {
                            StringBuilder sb = new StringBuilder();
                            for (int j = i; j < gameTitleGrammar.TitleWords.Count; j++)
                            {
                                sb.Append($"{gameTitleGrammar.TitleWords[j]} ");
                                if (!GameTitleGrammar.IsNoiseWord(sb.ToString().Trim()))
                                {
                                    GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, sb.ToString().Trim(), TitleMatchType.FullTitleContains, gameTitleGrammar.Title));
                                }
                            }
                        }
                    }
                });

                // create the voice recognition
                CreateRecognizer();

                // create the list of options
                SetupCategoryList();

                // prepare lists of games by different categories
                GameListSets = new List<GameListSet>();

                // populate the lists 
                CreateGameLists();

                // flag initialization complete - display results
                IsInitializing = false;
                IsDisplayingResults = true;

                // default to display games by platform
                ResetGameLists(ListCategoryType.Platform);

                // start off with a random game
                DoRandomGame(GetRandomFavoriteIndex());
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Initializion_loadData");
            }
        }

        public GameFiles GetNextGameFileCurrentList()
        {
            if(CurrentGameList?.MatchingGames == null)
            {
                return null;
            }

            for(int i = CurrentGameList.CurrentGameIndex; i < CurrentGameList.MatchingGames.Count; i++)
            {
                GameMatch match = CurrentGameList.MatchingGames[i];
                if(match?.GameFiles != null && !match.GameFiles.IsSetup)
                {
                    return (match.GameFiles);
                }
            }

            for(int i = 0; i < CurrentGameList.CurrentGameIndex; i++)
            {
                GameMatch match = CurrentGameList.MatchingGames[i];
                if (match?.GameFiles != null && !match.GameFiles.IsSetup)
                {
                    return (match.GameFiles);
                }
            }

            return null;
        }

        public GameFiles GetNextGameFileNextList()
        {
            if (NextGameList?.MatchingGames == null)
            {
                return null;
            }

            for (int i = NextGameList.CurrentGameIndex; i < NextGameList.MatchingGames.Count; i++)
            {
                GameMatch match = NextGameList.MatchingGames[i];
                if (match?.GameFiles != null && !match.GameFiles.IsSetup)
                {
                    return (match.GameFiles);
                }
            }

            for (int i = 0; i < NextGameList.CurrentGameIndex; i++)
            {
                GameMatch match = NextGameList.MatchingGames[i];
                if (match?.GameFiles != null && !match.GameFiles.IsSetup)
                {
                    return (match.GameFiles);
                }
            }

            return null;
        }

        public GameFiles GetNextGameFileAnyGame()
        {
            GameFiles gameFiles = null;
            if (GameFilesBag != null)
            {
                var anyGameQuery =
                    from g in GameFilesBag
                    where g.IsSetup == false
                    select g;
                gameFiles = anyGameQuery?.FirstOrDefault();
                if (gameFiles != null)
                {
                    return (gameFiles);
                }
            }

            return null;
        }

        // get a game whose images are not yet loaded 
        public GameFiles GetNextGameToLoad()
        {
            GameFiles gameFiles = null;

            if (CurrentGameList?.MatchingGames != null)
            {
                var currentListQuery =
                    from g in CurrentGameList.MatchingGames
                    where g.GameFiles.IsSetup == false
                    && CurrentGameList.MatchingGames.IndexOf(g) >= CurrentGameList.CurrentGameIndex
                    select g;

                gameFiles = currentListQuery?.FirstOrDefault()?.GameFiles;
                if (gameFiles != null)
                {
                    return (gameFiles);
                }
            }

            if (NextGameList?.MatchingGames != null)
            {
                var nextListQuery =
                    from g in NextGameList.MatchingGames
                    where g.GameFiles.IsSetup == false
                    && NextGameList.MatchingGames.IndexOf(g) >= NextGameList.CurrentGameIndex
                    select g;
                gameFiles = nextListQuery?.FirstOrDefault()?.GameFiles;
                if (gameFiles != null)
                {
                    return (gameFiles);
                }
            }

            if (GameFilesBag != null)
            {
                var anyGameQuery =
                    from g in GameFilesBag
                    where g.IsSetup == false
                    select g;
                gameFiles = anyGameQuery?.FirstOrDefault();
                if (gameFiles != null)
                {
                    return (gameFiles);
                }
            }

            return null;
        }

        private void CreateGameLists()
        {
            GetGamesByListCategoryType(ListCategoryType.Platform, true, true);
            GetGamesByListCategoryType(ListCategoryType.ReleaseYear);
            GetGamesByListCategoryType(ListCategoryType.Genre);
            GetGamesByListCategoryType(ListCategoryType.Publisher);
            GetGamesByListCategoryType(ListCategoryType.Developer);
            GetGamesByListCategoryType(ListCategoryType.Series);
            GetGamesByListCategoryType(ListCategoryType.PlayMode);
            GetGamesByListCategoryType(ListCategoryType.Playlist, true, true);
        }

        private bool CreateRecognizer()
        {
            try
            {
                // get the distinct set of phrases that can be used with voice recognition
                List<string> titleElements = GameBag.Where(game => game.CategoryType == ListCategoryType.VoiceSearch)
                    .GroupBy(game => game.CategoryValue)
                    .Distinct()
                    .Select(gameMatch => gameMatch.Key)
                    .ToList();

                SpeechRecognizer = new SpeechRecognizer(titleElements, RecognizeCompleted);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "CreateRecognizer");
            }

            return (true);
        }

        void DoMoreLikeCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.Game1;
            if (currentGame != null)
            {
                List<GameList> moreLikeThisResults = new List<GameList>();

                // get lists for matching series
                var seriesGameListSetQuery = from gameListSet in GameListSets
                            where gameListSet.ListCategoryType == ListCategoryType.Series
                            select gameListSet;

                GameListSet seriesGameListSet = seriesGameListSetQuery?.FirstOrDefault();
                if (seriesGameListSet != null)
                {
                    foreach (string series in currentGame?.Game?.SeriesValues)
                    {
                        var seriesGameListQuery = from seriesGameList in seriesGameListSet.GameLists
                                     where seriesGameList.ListDescription.Equals(series, StringComparison.InvariantCultureIgnoreCase)
                                     select seriesGameList;

                        foreach(GameList gameList in seriesGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }


                // get lists for matching genres
                var genreGameListSetQuery = from gameListSet in GameListSets
                                            where gameListSet.ListCategoryType == ListCategoryType.Genre
                                            select gameListSet;

                GameListSet genreGameListSet = genreGameListSetQuery?.FirstOrDefault();
                if(genreGameListSet != null)
                {
                    foreach(string genre in currentGame?.Game?.Genres)
                    {
                        var genreGameListQuery = from genreGameList in genreGameListSet.GameLists
                                                 where genreGameList.ListDescription.Equals(genre, StringComparison.InvariantCultureIgnoreCase)
                                                 select genreGameList;

                        foreach(GameList gameList in genreGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }


                // get platform list 
                var platformGameListSetQuery = from gameListSet in GameListSets
                                            where gameListSet.ListCategoryType == ListCategoryType.Platform
                                            select gameListSet;

                GameListSet platformGameListSet = platformGameListSetQuery?.FirstOrDefault();
                if (platformGameListSet != null)
                {
                    string platform = currentGame?.Game?.Platform;
                    if (platform != null)
                    {
                        var platformGameListQuery = from platformGameList in platformGameListSet.GameLists
                                                 where platformGameList.ListDescription.Equals(platform, StringComparison.InvariantCultureIgnoreCase)
                                                 select platformGameList;

                        foreach (GameList gameList in platformGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Developer list 
                var developerGameListSetQuery = from gameListSet in GameListSets
                                            where gameListSet.ListCategoryType == ListCategoryType.Developer
                                            select gameListSet;

                GameListSet developerGameListSet = developerGameListSetQuery?.FirstOrDefault();
                if (developerGameListSet != null)
                {
                    foreach (string developer in currentGame?.Game?.Developers)
                    {
                        var developerGameListQuery = from developerGameList in developerGameListSet.GameLists
                                                 where developerGameList.ListDescription.Equals(developer, StringComparison.InvariantCultureIgnoreCase)
                                                 select developerGameList;

                        foreach (GameList gameList in developerGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Publisher list
                var publisherGameListSetQuery = from gameListSet in GameListSets
                                                where gameListSet.ListCategoryType == ListCategoryType.Publisher
                                                select gameListSet;

                GameListSet publisherGameListSet = publisherGameListSetQuery?.FirstOrDefault();
                if (publisherGameListSet != null)
                {
                    foreach (string publisher in currentGame?.Game?.Publishers)
                    {
                        var publisherGameListQuery = from publisherGameList in publisherGameListSet.GameLists
                                                     where publisherGameList.ListDescription.Equals(publisher, StringComparison.InvariantCultureIgnoreCase)
                                                     select publisherGameList;

                        foreach (GameList gameList in publisherGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Play mode list
                var playModeGameListSetQuery = from gameListSet in GameListSets
                                                where gameListSet.ListCategoryType == ListCategoryType.PlayMode
                                                select gameListSet;

                GameListSet playModeGameListSet = playModeGameListSetQuery?.FirstOrDefault();
                if (playModeGameListSet != null)
                {
                    foreach (string playMode in currentGame?.Game?.PlayModes)
                    {
                        var playModeGameListQuery = from playModeGameList in playModeGameListSet.GameLists
                                                     where playModeGameList.ListDescription.Equals(playMode, StringComparison.InvariantCultureIgnoreCase)
                                                     select playModeGameList;

                        foreach (GameList gameList in playModeGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Release year list
                var releaseYearGameListSetQuery = from gameListSet in GameListSets
                                                where gameListSet.ListCategoryType == ListCategoryType.ReleaseYear
                                                select gameListSet;

                GameListSet releaseYearGameListSet = releaseYearGameListSetQuery?.FirstOrDefault();
                if (releaseYearGameListSet != null)
                {
                    int? releaseYear = currentGame?.Game?.ReleaseDate?.Year;
                    if(releaseYear != null)
                    {
                        var releaseYearGameListQuery = from releaseYearGameList in releaseYearGameListSet.GameLists
                                                     where releaseYearGameList.ListDescription.Equals(releaseYear.ToString(), StringComparison.InvariantCultureIgnoreCase)
                                                     select releaseYearGameList;

                        foreach (GameList gameList in releaseYearGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // remove any prior "more like this" set and then add these results in the more like this category
                GameListSets.RemoveAll(set => set.ListCategoryType == ListCategoryType.MoreLikeThis);
                GameListSets.Add(new GameListSet
                {
                    ListCategoryType = ListCategoryType.MoreLikeThis,
                    GameLists = moreLikeThisResults
                });

                ResetGameLists(ListCategoryType.MoreLikeThis);
                IsDisplayingResults = true;
                IsDisplayingFeature = false;
                IsDisplayingMoreInfo = false;
                CallGameChangeFunction();
            }
        }



        public void DoRecognize()
        {
            // bail out if the recognizer didn't get setup properly
            if (SpeechRecognizer == null)
                return;

            // bail out if already recognizing speech
            if (IsRecognizing)
                return;

            // bail out if still initializing
            if (IsInitializing)
                return;

            // stop any video or animations
            CallStopVideoAndAnimationsFunction();

            // flag recognizing and disable error, settings (category) and rating game
            IsRecognizing = true;
            IsRatingGame = false;
            IsPickingCategory = false;
            IsDisplayingError = false;

            SpeechRecognizer.DoSpeechRecognition();
        }

        void RecognizeCompleted(SpeechRecognizerResult speechRecognizerResult)
        {
            IsRecognizing = false;
            List<GameList> voiceRecognitionResults = new List<GameList>();

            if(!string.IsNullOrWhiteSpace(speechRecognizerResult.ErrorMessage))
            {
                ErrorMessage = speechRecognizerResult.ErrorMessage;
                return;
            }

            // speech hypothesized adds phrases that it heard to the TempGameLists collection
            // for each phrase, get the set of matching games from the GameTitlePhrases dictionary
            if (speechRecognizerResult?.RecognizedPhrases?.Count() > 0)
            {
                // in case the same phrase was recognized multiple times, group by phrase and keep only the max confidence
                var distinctGameLists = speechRecognizerResult?.RecognizedPhrases
                    .GroupBy(s => s.Phrase)
                    .Select(s => new GameList { ListDescription = s.Key, Confidence = s.Max(m => m.Confidence) }).ToList();

                // loop through the gamelists (one list for each hypothesized phrase)
                foreach (var gameList in distinctGameLists)
                {
                    // get the list of matching games for the phrase from the GameTitlePhrases dictionary 
                    var query = from game in GameBag
                                where game.CategoryType == ListCategoryType.VoiceSearch
                                && game.CategoryValue == gameList.ListDescription
                                group game by game into grouping
                                select GameMatch.CloneGameMatch(grouping.Key, ListCategoryType.VoiceSearch, gameList.ListDescription, grouping.Max(g => g.TitleMatchType), grouping.Key.ConvertedTitle);

                    if (query.Any())
                    {
                        List<GameMatch> matches = query.ToList();

                        foreach (var game in matches)
                        {
                            game.SetupVoiceMatchPercentage(gameList.Confidence, gameList.ListDescription);
                        }
                        gameList.MatchingGames = matches.OrderByDescending(match => match.MatchPercentage).ToList();
                        voiceRecognitionResults.Add(gameList);
                    }
                }
            }

            // remove any prior voice search set and then add these results in the voice search category
            GameListSets.RemoveAll(set => set.ListCategoryType == ListCategoryType.VoiceSearch);
            GameListSets.Add(new GameListSet
            {
                ListCategoryType = ListCategoryType.VoiceSearch,
                GameLists = voiceRecognitionResults
                                .OrderByDescending(list => list.MaxMatchPercentage)
                                .ThenByDescending(list => list.MaxTitleLength)
                                .ToList()
            });

            // display voice search results
            ResetGameLists(ListCategoryType.VoiceSearch);
            IsDisplayingResults = true;
            CallGameChangeFunction();
        }


        public int preAttractModeGameIndex { get; set; }
        public void SetPreAttractModeGame()
        {
            preAttractModeGameIndex = currentGameList.ListSetStartIndex + currentGameList.CurrentGameIndex - 1;
        }

        public void NextAttractModeGame()
        {
            int randomIndex = random.Next(0, CurrentGameListSet.TotalGameCount);

            // find the index of which list it's in 
            for (int listIndex = 0; listIndex < CurrentGameListSet.GameLists.Count; listIndex++)
            {
                GameList gameList = CurrentGameListSet.GameLists[listIndex];

                if (gameList.ListSetStartIndex <= randomIndex && gameList.ListSetEndIndex >= randomIndex)
                {
                    // once found, cycle to that list
                    listCycle.SetCurrentIndex(listIndex);

                    // refresh the game lists so we can get a handle on the current list
                    CurrentGameList = listCycle.GetItem(0);
                    NextGameList = listCycle.GetItem(1);

                    // setup the game list to the random game index 
                    CurrentGameList.SetGameIndex(randomIndex - gameList.ListSetStartIndex);
                    break;
                }
            }
        }

        private void RefreshGameLists()
        {
            if (listCycle?.GenericList?.Count == 0)
            {
                IsDisplayingError = true;

                // TODO: better message/handling of potential problems
                ErrorMessage = "A problem occurred trying to refresh the list of games";
                return;
            }

            CurrentGameList = listCycle.GetItem(0);
            NextGameList = listCycle.GetItem(1);
            CallGameChangeFunction();
        }

        private void CallGameChangeFunction()
        {
            if (GameChangeFunction != null)
            {
                GameChangeFunction();
            }
        }

        private void CallFeatureChangeFunction()
        {
            if (FeatureChangeFunction != null)
            {
                FeatureChangeFunction();
            }
        }

        private void CallStopVideoAndAnimationsFunction()
        {
            if(StopVideoAndAnimationsFunction != null)
            {
                StopVideoAndAnimationsFunction();
            }
        }

        private void CallUpdateRatingImageFunction()
        {
            if(UpdateRatingImageFunction != null)
            {
                UpdateRatingImageFunction();
            }
        }

        private void CycleListBackward()
        {
            listCycle.CycleBackward();
            RefreshGameLists();
        }

        private void CycleListForward()
        {
            listCycle.CycleForward();
            RefreshGameLists();
        }

        public void DoUp(bool held)
        {
            try
            {
                // stop displaying error
                if (IsDisplayingError)
                {
                    IsDisplayingError = false;
                    return;
                }

                // stop displaying attract mode 
                if (IsDisplayingAttractMode)
                {
                    IsDisplayingAttractMode = false;
                    DoRandomGame(preAttractModeGameIndex);
                    return;
                }

                if (IsPickingCategory)
                {
                    OptionList.CycleBackward();
                    return;
                }

                if (IsDisplayingMoreInfo)
                {
                    switch (GameDetailOption)
                    {
                        case GameDetailOption.Play:
                            GameDetailOption = GameDetailOption.Rating;
                            IsRatingGame = true;
                            break;

                        case GameDetailOption.Favorite:
                            GameDetailOption = GameDetailOption.Play;
                            break;

                        case GameDetailOption.MoreLikeThis:
                            GameDetailOption = GameDetailOption.Favorite;
                            break;

                        case GameDetailOption.Rating:
                            GameDetailOption = GameDetailOption.MoreLikeThis;
                            IsRatingGame = false;
                            SaveRatingCurrentGame();
                            break;
                    }
                    return;
                }

                if (IsDisplayingResults)
                {
                    // if displaying first list 
                    if ((listCycle.GetIndexValue(0) == 0))
                    {
                        // if it's not displaying featured - change to display feature mode
                        if (!IsDisplayingFeature)
                        {
                            IsDisplayingFeature = true;
                            CallFeatureChangeFunction();
                            return;
                        }
                        // if it is displaying feature mode - change to regular results mode and go to prior list
                        else
                        {
                            IsDisplayingFeature = false;
                            CallFeatureChangeFunction();
                            CycleListBackward();
                            return;
                        }
                    }

                    if (IsDisplayingFeature)
                    {
                        IsDisplayingFeature = false;
                        CallFeatureChangeFunction();
                        return;
                    }

                    // cycle to prior list
                    CycleListBackward();
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "DoUp");
            }
        }

        public void DoDown(bool held)
        {
            try
            {
                // stop displaying error
                if (IsDisplayingError)
                {
                    IsDisplayingError = false;
                    return;
                }

                if (IsDisplayingAttractMode)
                {
                    IsDisplayingAttractMode = false;
                    DoRandomGame(preAttractModeGameIndex);
                    return;
                }

                if (isPickingCategory)
                {
                    OptionList.CycleForward();
                    return;
                }

                if (isDisplayingMoreInfo)
                {
                    switch (GameDetailOption)
                    {
                        case GameDetailOption.Play:
                            GameDetailOption = GameDetailOption.Favorite;
                            break;

                        case GameDetailOption.Favorite:
                            GameDetailOption = GameDetailOption.MoreLikeThis;
                            break;

                        case GameDetailOption.MoreLikeThis:
                            GameDetailOption = GameDetailOption.Rating;
                            IsRatingGame = true;
                            break;

                        case GameDetailOption.Rating:
                            GameDetailOption = GameDetailOption.Play;
                            IsRatingGame = false;
                            SaveRatingCurrentGame();
                            break;
                    }
                    return;
                }

                if (isDisplayingFeature)
                {
                    IsDisplayingFeature = false;
                    CallFeatureChangeFunction();
                    return;
                }

                if (isDisplayingResults)
                {
                    // cycle to next list
                    CycleListForward();
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "DoDown");
            }
        }

        public void DoLeft(bool held)
        {
            try
            {
                if (IsDisplayingAttractMode)
                {
                    IsDisplayingAttractMode = false;
                    DoRandomGame(preAttractModeGameIndex);
                    return;
                }

                // stop displaying error
                if (IsDisplayingError)
                {
                    IsDisplayingError = false;
                    return;
                }

                // if picking category and going left then close the category and keep going left
                if (IsPickingCategory)
                {
                    // todo: playing with not letting go left from the settings/category selection
                    //IsPickingCategory = false;
                    //CurrentGameList.CycleBackward();
                    //CallGameChangeFunction();
                    return;
                }

                if (IsRatingGame)
                {
                    // increment the game rating by 0.1
                    RateCurrentGame(-0.5f);
                    return;
                }

                // don't do anything when displaying more info
                if(IsDisplayingMoreInfo)
                {
                    return;
                }

                if (IsDisplayingFeature)
                {
                    // toggle the feature option
                    if (FeatureOption == FeatureGameOption.MoreInfo)
                    {
                        FeatureOption = FeatureGameOption.PlayGame;
                        return;
                    }

                    // open the settings/category menu
                    if (FeatureOption == FeatureGameOption.PlayGame)
                    {
                        // display category options
                        IsPickingCategory = true;
                        return;
                    }
                }

                // if current game is the first game then going left displays the category options
                if (IsDisplayingResults && CurrentGameList.CurrentGameIndex == 0)
                {
                    IsPickingCategory = true;
                    return;
                }

                // cycle left to prior game if 1st game is other than index 0 Index1 != 0
                if (IsDisplayingResults && CurrentGameList.CurrentGameIndex != 0)
                {
                    CurrentGameList.CycleBackward();
                    CallGameChangeFunction();
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "DoLeft");
            }
        }

        public void DoRight(bool held)
        {
            try
            {
                // stop displaying error
                if (IsDisplayingError)
                {
                    IsDisplayingError = false;
                    return;
                }

                // stop displaying attract mode 
                if (IsDisplayingAttractMode)
                {
                    IsDisplayingAttractMode = false;
                    DoRandomGame(preAttractModeGameIndex);
                    return;
                }
                
                // if picking category, going right closes category selection
                if (IsPickingCategory)
                {
                    IsPickingCategory = false;
                    CallGameChangeFunction();
                    return;
                }

                if(IsRatingGame)
                {
                    // increment the game rating by 0.1
                    RateCurrentGame(0.5f);
                    return;
                }

                // don't do anything when displaying more info
                if (IsDisplayingMoreInfo)
                {
                    return;
                }

                // toggle the feature option
                if (IsDisplayingFeature)
                {
                    if (FeatureOption == FeatureGameOption.PlayGame)
                    {
                        FeatureOption = FeatureGameOption.MoreInfo;
                    }
                    return;
                }

                if (IsDisplayingResults)
                {
                    // cycle right to next game
                    CurrentGameList.CycleForward();
                    CallGameChangeFunction();
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "DoRight");
            }
        }

        public void DoPageUp()
        {
            // stop displaying error
            if (IsDisplayingError)
            {
                IsDisplayingError = false;
                return;
            }

            // stop displaying attract mode 
            if (IsDisplayingAttractMode)
            {
                IsDisplayingAttractMode = false;
                DoRandomGame(preAttractModeGameIndex);
                return;
            }

            DoRandomGame();
        }

        public void DoPageDown()
        {
            // stop displaying error
            if (IsDisplayingError)
            {
                IsDisplayingError = false;
                return;
            }

            // stop displaying attract mode 
            if (IsDisplayingAttractMode)
            {
                IsDisplayingAttractMode = false;
                DoRandomGame(preAttractModeGameIndex);
                return;
            }

            DoRecognize();
        }

        private void ResetGameLists(ListCategoryType listCategoryType)
        {
            // get the game list from the GameListSet for the given listCategoryType
            var query = from gameListSet in GameListSets
                        where gameListSet.ListCategoryType == listCategoryType
                        select gameListSet;

            CurrentGameListSet = query?.FirstOrDefault();
            if (CurrentGameListSet != null)
            {
                listCycle = new ListCycle<GameList>(CurrentGameListSet.GameLists, 2);
                RefreshGameLists();
            }
        }

        private static readonly Random random = new Random();
        public void DoRandomGame(int randomIndex = -1)
        {
            // get a game index from the current list set
            if (randomIndex == -1)
            {
                randomIndex = random.Next(0, CurrentGameListSet.TotalGameCount);
            }

            // find the index of which list it's in 
            for (int listIndex = 0; listIndex < CurrentGameListSet.GameLists.Count; listIndex++)
            {
                GameList gameList = CurrentGameListSet.GameLists[listIndex];

                if (gameList.ListSetStartIndex <= randomIndex && gameList.ListSetEndIndex >= randomIndex)
                {
                    // once found, cycle to that list
                    listCycle.SetCurrentIndex(listIndex);

                    // refresh the game lists so we can get a handle on the current list
                    CurrentGameList = listCycle.GetItem(0);
                    NextGameList = listCycle.GetItem(1);

                    // setup the game list to the random game index 
                    CurrentGameList.SetGameIndex(randomIndex - gameList.ListSetStartIndex);
                    break;
                }
            }

            // call the game change function to refresh things
            CallGameChangeFunction();
        }

        private int GetRandomFavoriteIndex()
        {
            var q = from list in CurrentGameListSet.GameLists
                    where list.ListDescription.Equals("Favorites", StringComparison.InvariantCultureIgnoreCase)
                    select list;

            GameList favoritesList = q?.FirstOrDefault();
            if(favoritesList != null)
            {
                return random.Next(0, favoritesList.MatchCount);
            }
            return (-1);
        }

        // start the current game
        private void PlayCurrentGame()
        {
            // get a handle on the current game 
            IGame currentGame = CurrentGameList?.Game1?.Game;
            if (currentGame != null)
            {
                // check if the game is in the history list already
                var historyQuery = GameBag.Where(g => g.Game.Id == currentGame.Id && g.CategoryType == ListCategoryType.History);
                if (!historyQuery.Any())
                {
                    // setting the last played date will make it show up in the history after launching a game
                    var game = CurrentGameList?.Game1?.Game;
                    if (game != null)
                    {
                        game.LastPlayedDate = DateTime.Now;
                    }

                    // if not - add it to the history list
                    GameBag.Add(GameMatch.CloneGameMatch(CurrentGameList?.Game1, ListCategoryType.History, ListCategoryType.History.ToString()));
                }
                else
                {
                    // if it is - update the last played time to now
                    foreach(var game in historyQuery)
                    {
                        game.Game.LastPlayedDate = DateTime.Now;
                    }
                }

                // reset the lists so the updated history reflects - first save the current game details then reload the lists 
                saveStateForGameListChange();
                resetListsAfterChange();

                // stop everything in the UI
                CallStopVideoAndAnimationsFunction();

                // launch the game 
                PluginHelper.BigBoxMainViewModel.PlayGame(currentGame, null, null, null);
            }
            return;
        }

        // mark current game as a favorite
        private void FavoriteCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.Game1;
            if(currentGame != null)
            {
                currentGame.Favorite = !currentGame.Favorite;
                
                PluginHelper.DataManager.Save(false);

                var gameMatchQuery = from gameMatch in GameBag
                                     where gameMatch.Game.Id == currentGame.Game.Id
                                     select gameMatch;

                // flag the game as a favorite wherever it appears
                foreach(var gameMatch in gameMatchQuery)
                {
                    gameMatch.Favorite = currentGame.Favorite;
                }

                gameMatchQuery = GameBag.Where(g => g.Favorite && g.Game.Id == currentGame.Game.Id && g.CategoryType == ListCategoryType.Favorites);

                // add to the game bag in the favorites category 
                if (currentGame.Favorite && !gameMatchQuery.Any())
                {
                    GameBag.Add(GameMatch.CloneGameMatch(currentGame, ListCategoryType.Favorites, ListCategoryType.Favorites.ToString()));
                }

                // save state so we can get back to the current game
                saveStateForGameListChange();
            }
        }

        // variables to track what list set, list, and game we were on when a game is favorited
        // save current list set type - i.e. lists by platform, genre, publisher, etc...
        // save the current list type - generally would match the list set unless it's favorites
        // identifies which list we are in within the list set - would be better if we created a guid to identify these
        // get the game id that we are on 
        // get the starting index for the list within the list set
        // get the index of the game within the list 
        private bool gameListsChanged;
        private ListCategoryType preChangeListSetCategoryType;
        private ListCategoryType preChangeListCategoryType;
        private string preChangeListDescription;
        private string preChangeGameId;
        private int preChangeGameListStartIndex;
        private int preChangeGameIndex;

        // call this when lists are about to change to save which list set, list, and game we were on so we can find our way back after rebuilding lists
        // this is needed when game lists are going to change (i.e. adding/removing favorites, adding/removing from history)
        private void saveStateForGameListChange()
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

            // get the starting index for the list within the list set
            preChangeGameListStartIndex = currentGameList.ListSetStartIndex;

            // get the index of the game within the list 
            preChangeGameIndex = currentGameList.CurrentGameIndex;
        }

        // call this when lists have changed (i.e. game added/removed from favorites history list)
        // will try to find the game in the same list - if it can't (i.e. in favorites and game removed from favorites) then jumps to the next game
        private void resetListsAfterChange()
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
            if(gameList != null)
            {
                // try to find the game in the list
                var gameMatchQuery = from match in gameList.MatchingGames
                                     where match.Game.Id == preChangeGameId
                                     select match;

                var gameMatch = gameMatchQuery?.FirstOrDefault();
                if(gameMatch != null)
                {
                    // the game is in the list 
                    int gameIndex = gameList.MatchingGames.FindIndex(mat => mat.Game.Id == preChangeGameId);
                    if(gameIndex >= 0)
                    {
                        // jump to the game
                        DoRandomGame(gameList.ListSetStartIndex + gameIndex);
                        return;
                    }
                }

                // the game was not in the list so try the next game in the list 
                if(gameList?.MatchingGames?.Count() > preChangeGameIndex)
                {
                    DoRandomGame(gameList.ListSetStartIndex + preChangeGameIndex);
                    return;
                }

                // there was no next game so try a previous game in the list 
                if(gameList?.MatchingGames?.Count() > preChangeGameIndex - 1)
                {
                    DoRandomGame(gameList.ListSetStartIndex + preChangeGameIndex - 1);
                    return;
                }

                // there was no next or previous, try just the first game in the list 
                if (gameList?.MatchingGames?.Count() > 0)
                {
                    DoRandomGame(gameList.ListSetStartIndex);
                    return;
                }
            }
            else
            {
                // the list was not there so pick any random game
                DoRandomGame();
                return;
            }
        }


        private void RateCurrentGame(float changeAmount)
        {
            GameMatch currentGame = CurrentGameList?.Game1;
            if(currentGame != null)
            {
                float newRating = currentGame.UserRating + changeAmount;
                if (newRating > 5) newRating = 5.0f;
                if (newRating < 0) newRating = 0.0f;

                currentGame.UserRating = newRating;
            }
        }

        private void SaveRatingCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.Game1;
            if (currentGame != null)
            {
                // save the rating change to the launchbox data 
                PluginHelper.DataManager.Save(false);

                // reload the game's start rating image
                currentGame.GameFiles.ResetStarRatingImage();

                // trigger the view to update the image
                CallUpdateRatingImageFunction();
            }
        }


        public void DoEnter()
        {
            // stop displaying error
            if (IsDisplayingError)
            {
                IsDisplayingError = false;
                return;
            }

            // stop displaying attract mode 
            if (IsDisplayingAttractMode)
            {
                IsDisplayingAttractMode = false;
                CallGameChangeFunction();
                return;
            }

            if (IsPickingCategory)
            {
                IsPickingCategory = false;

                Option<ListCategoryType> option = OptionList.Option0;
                switch (option.EnumOption)
                {
                    case ListCategoryType.VoiceSearch:
                        DoRecognize();
                        break;

                    case ListCategoryType.RandomGame:
                        DoRandomGame();
                        break;

                    case ListCategoryType.ReleaseYear:
                        ResetGameLists(ListCategoryType.ReleaseYear);
                        break;

                    case ListCategoryType.Platform:
                        ResetGameLists(ListCategoryType.Platform);
                        break;

                    case ListCategoryType.Developer:
                        ResetGameLists(ListCategoryType.Developer);
                        break;

                    case ListCategoryType.Genre:
                        ResetGameLists(ListCategoryType.Genre);
                        break;

                    case ListCategoryType.Playlist:
                        ResetGameLists(ListCategoryType.Playlist);
                        break;

                    case ListCategoryType.PlayMode:
                        ResetGameLists(ListCategoryType.PlayMode);
                        break;

                    case ListCategoryType.Publisher:
                        ResetGameLists(ListCategoryType.Publisher);
                        break;

                    case ListCategoryType.Series:
                        ResetGameLists(ListCategoryType.Series);
                        break;
                }

                return;
            }

            if (IsDisplayingMoreInfo)
            {
                switch (GameDetailOption)
                {
                    case GameDetailOption.Play:
                        PlayCurrentGame();
                        break;

                    case GameDetailOption.Favorite:
                        FavoriteCurrentGame();
                        break;

                    case GameDetailOption.Rating:
                        if (IsRatingGame)
                        {
                            SaveRatingCurrentGame();
                        }
                        break;

                    case GameDetailOption.MoreLikeThis:
                        DoMoreLikeCurrentGame();
                        break;
                }
                return;
            }

            if (IsDisplayingFeature && FeatureOption == FeatureGameOption.MoreInfo)
            {
                GameDetailOption = GameDetailOption.Play;
                IsDisplayingMoreInfo = true;
                return;
            }

            if (isDisplayingFeature && FeatureOption == FeatureGameOption.PlayGame)
            {
                // start the current game
                PlayCurrentGame();
                return;
            }

            if (isDisplayingResults)
            {
                GameDetailOption = GameDetailOption.Play;
                IsDisplayingFeature = true;
                IsDisplayingMoreInfo = true;
                return;
            }
        }

        public bool DoEscape()
        {
            // stop displaying error
            if (IsDisplayingError)
            {
                IsDisplayingError = false;
                return true;
            }

            // stop displaying attract mode 
            if (IsDisplayingAttractMode)
            {
                IsDisplayingAttractMode = false;
                DoRandomGame(preAttractModeGameIndex);
                return true;
            }

            if (IsDisplayingMoreInfo)
            {
                if (gameListsChanged)
                {
                    resetListsAfterChange();
                }

                IsDisplayingMoreInfo = false;
                IsDisplayingFeature = false;
                IsRatingGame = false;
                return true;
            }

            // let bigbox handle escape and go to the bigbox menu
            if (IsPickingCategory)
            {
                return false;
            }

            // if displaying results - open the settings 
            if (IsDisplayingResults)
            {
                IsPickingCategory = true;
                return true;
            }

            // let bigbox handle escape and go to the bigbox menu
            return false;
        }

        private GameList currentGameList;
        public GameList CurrentGameList
        {
            get { return currentGameList; }
            set
            {
                {
                    currentGameList = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentGameList"));
                }
            }
        }

        private GameList nextGameList;
        public GameList NextGameList
        {
            get { return nextGameList; }
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
                setButtonImages();
            }
        }

        private void setButtonImages()
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
            get
            {
                return playButtonImage;
            }
            set
            {
                if(playButtonImage != value)
                {
                    playButtonImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PlayButtonImage"));
                }
            }
        }

        private Uri moreInfoImage;
        public Uri MoreInfoImage
        {
            get
            {
                return moreInfoImage;
            }

            set
            {
                if(moreInfoImage != value)
                {
                    moreInfoImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("MoreInfoImage"));
                }
            }
        }

        public int Star1 { get { return 1; } }
        public int Star2 { get { return 2; } }
        public int Star3 { get { return 3; } }
        public int Star4 { get { return 4; } }
        public int Star5 { get { return 5; } }

        public float StarOffset00 { get { return 0.0f; } }
        public float StarOffset01 { get { return 0.1f; } }
        public float StarOffset02 { get { return 0.2f; } }
        public float StarOffset03 { get { return 0.3f; } }
        public float StarOffset04 { get { return 0.4f; } }
        public float StarOffset05 { get { return 0.5f; } }
        public float StarOffset06 { get { return 0.6f; } }
        public float StarOffset07 { get { return 0.7f; } }
        public float StarOffset08 { get { return 0.8f; } }
        public float StarOffset09 { get { return 0.9f; } }
        public float StarOffset10 { get { return 1.0f; } }

        public event PropertyChangedEventHandler PropertyChanged = delegate {};
    }
}