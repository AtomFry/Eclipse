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
using System.Speech.Recognition;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Media.TextFormatting;

namespace Eclipse.View
{
    public delegate void FeatureChangeFunction();
    public delegate void AnimateGameChangeFunction();
    
    public delegate void IncrementLoadingProgressFunction();
    public delegate void StopVideoAndAnimations();

    class MainWindowViewModel : INotifyPropertyChanged
    {
        private List<Option<ListCategoryType>> listCategories;
        private ListCycle<GameList> listCycle;
        public List<GameListSet> GameListSets { get; set; }

        private List<GameList> TempGameLists { get; set; }
        private SpeechRecognitionEngine Recognizer { get; set; }

        private ConcurrentBag<GameMatch> GameBag = new ConcurrentBag<GameMatch>();

        // store platform bezels and default bezels in dictionary for easy lookup
        // this is probably terrible design but the problem is I won't know the video orientation in the GameMatch, I get that once the MediaElement is loaded over in the view
        // TODO: add game title (or id) here to take care of all game bezels too - currently game specific bezels are loaded up in the GameMatch
        // Key: <BezelType, BezelOrientation, Platform> 
        private Dictionary<Tuple<BezelType, BezelOrientation, string>, Uri> BezelDictionary;

        public FeatureChangeFunction FeatureChangeFunction { get; set; }
        public AnimateGameChangeFunction GameChangeFunction { get; set; }
        public StopVideoAndAnimations StopVideoAndAnimationsFunction { get; set; }

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


        private int totalGameCount;
        public int TotalGameCount
        {
            get { return totalGameCount; }
            set
            {
                if (totalGameCount != value)
                {
                    totalGameCount = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("TotalGameCount"));
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
        private int initializationGameCount;
        public int InitializationGameCount
        {
            get { return initializationGameCount; }
            set
            {
                if (initializationGameCount != value)
                {
                    initializationGameCount = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("InitializationGameCount"));
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
            listCategories.Add(new Option<ListCategoryType> { Name = "Voice search", EnumOption = ListCategoryType.VoiceSearch, SortOrder = 5, ShortDescription = "Voice", LongDescription = "Voice search" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Play mode", EnumOption = ListCategoryType.PlayMode, SortOrder = 6, ShortDescription = "Mode", LongDescription = "Play mode" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Developer", EnumOption = ListCategoryType.Developer, SortOrder = 7, ShortDescription = "Dev", LongDescription = "Developer" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Publisher", EnumOption = ListCategoryType.Publisher, SortOrder = 8, ShortDescription = "Pub", LongDescription = "Publisher" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Year", EnumOption = ListCategoryType.ReleaseYear, SortOrder = 9, ShortDescription = "Year", LongDescription = "Release year" });
            listCategories.Add(new Option<ListCategoryType> { Name = "Random", EnumOption = ListCategoryType.RandomGame, SortOrder = 10, ShortDescription = "Random", LongDescription = "Random game" });
            OptionList = new OptionList(listCategories);
        }

        // get platform bezels if game bezel is not there
        private void GetPlatformBezels()
        {
            // plaform bezel path
            // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\{PLATFORM}\Bezel\Horizontal.png
            // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\{PLATFORM}\Bezel\Vertical.png

            List<IPlatform> platforms = new List<IPlatform>(PluginHelper.DataManager.GetAllPlatforms());
            foreach (var platform in platforms)
            {
                Tuple<BezelType, BezelOrientation, string> platformHorizontalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.PlatformDefault, BezelOrientation.Horizontal, platform.Name);
                Tuple<BezelType, BezelOrientation, string> platformVerticalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.PlatformDefault, BezelOrientation.Vertical, platform.Name);

                string platformHorizontalBezelPath = Path.Combine(Helpers.PluginImagesPath, "Platforms", platform.Name, "Bezel", "Horizontal.png");
                string platformVerticalBezelPath = Path.Combine(Helpers.PluginImagesPath, "Platforms", platform.Name, "Bezel", "Vertical.png");

                if (File.Exists(platformHorizontalBezelPath)) BezelDictionary.Add(platformHorizontalBezelKey, new Uri(platformHorizontalBezelPath));
                if (File.Exists(platformVerticalBezelPath)) BezelDictionary.Add(platformVerticalBezelKey, new Uri(platformVerticalBezelPath));
            }
        }

        // get default bezels if game bezel and platform bezel is not there
        private void GetDefaultBezels()
        {
            // default bezel path
            // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\Default\Bezel\Horizontal.png
            // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\Default\Bezel\Vertical.png
            Tuple<BezelType, BezelOrientation, string> defaultHorizontalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.Default, BezelOrientation.Horizontal, string.Empty);
            Tuple<BezelType, BezelOrientation, string> defaultVerticalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.Default, BezelOrientation.Vertical, string.Empty);

            string defaultHorizontalBezelPath = Path.Combine(Helpers.PluginImagesPath, "Platforms", "Default", "Bezel", "Horizontal.png");
            string defaultVerticalBezelPath = Path.Combine(Helpers.PluginImagesPath, "Platforms", "Default", "Bezel", "Vertical.png");

            if (File.Exists(defaultHorizontalBezelPath)) BezelDictionary.Add(defaultHorizontalBezelKey, new Uri(defaultHorizontalBezelPath));
            if (File.Exists(defaultVerticalBezelPath)) BezelDictionary.Add(defaultVerticalBezelKey, new Uri(defaultVerticalBezelPath));
        }

        private void GetGamesByListCategoryType(ListCategoryType listCategoryType, bool includeFavorites = false)
        {
            List<GameList> listOfGameList = new List<GameList>();
            
            if(includeFavorites)
            {
                var favoriteGames = from gameMatch in GameBag
                                    where gameMatch.CategoryType == ListCategoryType.Favorites
                                    group gameMatch by gameMatch.CategoryValue into favoritesGroup
                                    select favoritesGroup;

                foreach (var favoritesGroup in favoriteGames)
                {
                    listOfGameList.Add(new GameList(favoritesGroup.Key, favoritesGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList(), true));
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
                GameLists = listOfGameList.OrderByDescending(list=>list.IsFavorites).ThenBy(list => list.ListDescription).ToList(),
                ListCategoryType = listCategoryType
            });
        }


        // todo: fix playlists 
        /*
        private void GetGamesByPlaylist()
        {
            
            IPlaylist[] allPlaylists = PluginHelper.DataManager.GetAllPlaylists();
            List<GameList> listOfPlayListGames = new List<GameList>();

            foreach (IPlaylist playlist in allPlaylists)
            {
                if (playlist.HasGames(false, false))
                {
                    IGame[] playlistGames = playlist.GetAllGames(true);
                    var orderedPlatformGames = playlistGames.OrderBy(game => game.SortTitleOrTitle);
                    var query = from game in orderedPlatformGames select new GameMatch(game, TitleMatchType.None);

                    GameList gameList = new GameList(playlist.SortTitleOrTitle, query.ToList());
                    gameList.ListCategoryType = ListCategoryType.Playlist;
                    listOfPlayListGames.Add(gameList);
                }
            }
            GameListSets.Add(new GameListSet { GameLists = listOfPlayListGames, ListCategoryType = ListCategoryType.Playlist });
        }
        */


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


        void Initialization_LoadData(object sender, DoWorkEventArgs e)
        {
            try
            {
                List<PlaylistGame> playlistGames = GetPlaylistGames();

                // get platform bezels
                GetPlatformBezels();
                GetDefaultBezels();

                // get the total count of games for the progress bar
                TotalGameCount = AllGames?.Count ?? 0;

                // prescale box front images - doing this here so it's easier to update the progress bar

                // get list of image files in launchbox folders that are missing from the plug-in folders
                List<FileInfo> gameFrontFilesToProcess = ImageScaler.GetMissingGameFrontImageFiles();
                List<FileInfo> platformLogosToProcess = ImageScaler.GetMissingPlatformClearLogoFiles();
                List<FileInfo> gameClearLogosToProcess = ImageScaler.GetMissingGameClearLogoFiles();

                // get the desired height of pre-scaled box images based on the monitor's resolution
                // only need this if we have anything to process
                int desiredHeight = 0;
                if ((gameFrontFilesToProcess.Count > 0) || (platformLogosToProcess.Count > 0) || (gameClearLogosToProcess.Count > 0))
                {
                    LoadingMessage = $"PRESCALING IMAGES";
                    desiredHeight = ImageScaler.GetDesiredHeight();
                }

                // add the count of missing files for the loading progress bar
                TotalGameCount += (gameFrontFilesToProcess?.Count ?? 0);
                TotalGameCount += (platformLogosToProcess?.Count ?? 0);
                TotalGameCount += (gameClearLogosToProcess?.Count ?? 0);
                InitializationGameCount = 0;

                // process the folders 
                foreach (FileInfo fileInfo in gameFrontFilesToProcess)
                {
                    InitializationGameCount += 1;
                    ImageScaler.ScaleImage(fileInfo, desiredHeight);
                }

                // crop platform clear logos 
                foreach (FileInfo fileInfo in platformLogosToProcess)
                {
                    InitializationGameCount += 1;
                    ImageScaler.CropImage(fileInfo);
                }

                // crop game clear logos 
                foreach (FileInfo fileInfo in gameClearLogosToProcess)
                {
                    InitializationGameCount += 1;
                    ImageScaler.CropImage(fileInfo);
                }

                LoadingMessage = null;

                Parallel.ForEach(AllGames, (game) =>
                {
                    InitializationGameCount += 1;

                    GameMatch gameMatch = new GameMatch(game);

                    if (game.Favorite)
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Favorites, ListCategoryType.Favorites.ToString()));
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
                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Title))
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammarBuilder.Title, TitleMatchType.FullTitleMatch, gameTitleGrammarBuilder.Title));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.MainTitle))
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammarBuilder.MainTitle, TitleMatchType.MainTitleMatch, gameTitleGrammarBuilder.Title));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Subtitle))
                    {
                        GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammarBuilder.Subtitle, TitleMatchType.SubtitleMatch, gameTitleGrammarBuilder.Title));
                    }

                    for (int i = 0; i < gameTitleGrammarBuilder.TitleWords.Count; i++)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int j = i; j < gameTitleGrammarBuilder.TitleWords.Count; j++)
                        {
                            sb.Append($"{gameTitleGrammarBuilder.TitleWords[j]} ");
                            if (!GameTitleGrammarBuilder.IsNoiseWord(sb.ToString().Trim()))
                            {
                                GameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, sb.ToString().Trim(), TitleMatchType.FullTitleContains, gameTitleGrammarBuilder.Title));
                            }
                        }
                    }
                });


                // todo: fix create voice recognizer for 11.3 and later
                CreateRecognizer();

                // create the list of options
                SetupCategoryList();

                // prepare lists of games by different categories
                GameListSets = new List<GameListSet>();

                GetGamesByListCategoryType(ListCategoryType.Platform, true);
                GetGamesByListCategoryType(ListCategoryType.ReleaseYear);
                GetGamesByListCategoryType(ListCategoryType.Genre);
                GetGamesByListCategoryType(ListCategoryType.Publisher);
                GetGamesByListCategoryType(ListCategoryType.Developer);
                GetGamesByListCategoryType(ListCategoryType.Series);
                GetGamesByListCategoryType(ListCategoryType.PlayMode);
                GetGamesByListCategoryType(ListCategoryType.Favorites);
                GetGamesByListCategoryType(ListCategoryType.Playlist, true);


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
                Helpers.LogException(ex, "Initializion_loadData");
            }
        }

        private bool CreateRecognizer()
        {
            try
            {
                List<string> titleElements = GameBag.Where(game => game.CategoryType == ListCategoryType.VoiceSearch)
                    .GroupBy(game => game.CategoryValue)
                    .Distinct()
                    .Select(gameMatch => gameMatch.Key)
                    .ToList();

                // add the distinct phrases to the list of choices
                Choices choices = new Choices();
                choices.Add(titleElements.ToArray());

                GrammarBuilder grammarBuilder = new GrammarBuilder();
                grammarBuilder.Append(choices);

                Grammar grammar = new Grammar(grammarBuilder)
                {
                    Name = "Game title elements"
                };

                // setup the recognizer
                Recognizer = new SpeechRecognitionEngine();
                Recognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(5.0);
                Recognizer.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(RecognizeCompleted);
                Recognizer.LoadGrammarAsync(grammar);
                Recognizer.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(SpeechHypothesized);
                Recognizer.SetInputToDefaultAudioDevice();
                Recognizer.RecognizeAsyncCancel();

            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, "CreateRecognizer");
            }

            return (true);
        }

        void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            // ignore noise words 
            if (!GameTitleGrammarBuilder.IsNoiseWord(e.Result.Text))
            {
                TempGameLists.Add(new GameList
                {
                    ListDescription = e.Result.Text,
                    Confidence = e.Result.Confidence
                });
            }
        }

        void RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            IsRecognizing = false;
            List<GameList> voiceRecognitionResults = new List<GameList>();

            if (e?.Error != null)
            {
                if (Recognizer != null)
                {
                    Recognizer.RecognizeAsyncCancel();
                }

                IsDisplayingError = true;
                ErrorMessage = e.Error.Message;
                return;
            }

            if (e?.InitialSilenceTimeout == true || e?.BabbleTimeout == true)
            {
                if (Recognizer != null)
                {
                    Recognizer.RecognizeAsyncCancel();
                }

                IsDisplayingError = true;
                ErrorMessage = "Voice recognition could not hear anything, please try again";
                return;
            }

            // speech hypothesized adds phrases that it heard to the TempGameLists collection
            // for each phrase, get the set of matching games from the GameTitlePhrases dictionary
            if (TempGameLists?.Count() > 0)
            {
                // in case the same phrase was recognized multiple times, group by phrase and keep only the max confidence
                var distinctGameLists = TempGameLists
                    .GroupBy(s => s.ListDescription)
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

                    if(query.Any())
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
            IsDisplayingFeature = true;
            CallGameChangeFunction();
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
                }) ;

                ResetGameLists(ListCategoryType.MoreLikeThis);
                IsDisplayingResults = true;
                IsDisplayingFeature = false;
                IsDisplayingMoreInfo = false;
                CallGameChangeFunction();
            }
        }



        public void DoRecognize()
        {
            if (IsRecognizing)
                return;

            if (IsInitializing)
                return;

            // stop any video or animations
            CallStopVideoAndAnimationsFunction();

            // flag recognizing and disable error, settings (category) and rating game
            IsRecognizing = true;
            IsRatingGame = false;
            IsPickingCategory = false;
            IsDisplayingError = false;

            // reset the results and the temporary results
            TempGameLists = new List<GameList>();

            // kick off voice recognition 
            Recognizer.RecognizeAsync(RecognizeMode.Single);
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
                    CallGameChangeFunction();
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
                Helpers.LogException(ex, "DoUp");
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
                    CallGameChangeFunction();
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
                Helpers.LogException(ex, "DoDown");
            }
        }

        public void DoLeft(bool held)
        {
            try
            {
                if (IsDisplayingAttractMode)
                {
                    IsDisplayingAttractMode = false;
                    CallGameChangeFunction();
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
                    // todo: play with not letting go left from the settings/category selection
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
                Helpers.LogException(ex, "DoLeft");
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
                    CallGameChangeFunction();
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
                Helpers.LogException(ex, "DoRight");
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
                CallGameChangeFunction();
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
                CallGameChangeFunction();
                return;
            }

            DoRecognize();
        }

        private void ResetGameLists(ListCategoryType listCategoryType)
        {
            // get the game list from the GameListSet for the given listCategoryType
            var query = from gameList in GameListSets
                        where gameList.ListCategoryType == listCategoryType
                        select gameList;

            CurrentGameListSet = query?.FirstOrDefault();
            if (CurrentGameListSet != null)
            {
                listCycle = new ListCycle<GameList>(CurrentGameListSet.GameLists, 2);
                RefreshGameLists();
                CallGameChangeFunction();
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
            IsDisplayingFeature = true;
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

        private void PlayCurrentGame()
        {
            // start the current game
            IGame currentGame = CurrentGameList?.Game1?.Game;
            if (currentGame != null)
            {
                CallStopVideoAndAnimationsFunction();
                PluginHelper.BigBoxMainViewModel.PlayGame(currentGame, null, null, null);
            }
            return;
        }

        private void FavoriteCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.Game1;
            if(currentGame != null)
            {
                currentGame.Favorite = !currentGame.Favorite;
                
                PluginHelper.DataManager.Save(false);


                // todo: add/remove game from favorites list
                /*
                foreach (GameListSet gameListSet in GameListSets)
                {
                    var query = from gameList in gameListSet.GameLists where gameList.ListCategoryType == ListCategoryType.Favorites select gameList;
                    foreach(GameList gameList in query)
                    {
                        List<GameMatch> favoritesList = gameList.MatchingGames;
                        if(currentGame.Favorite)
                        {
                            favoritesList.Add(currentGame);
                        }
                        else
                        {
                            if (favoritesList.Contains(currentGame))
                            {
                                favoritesList.Remove(currentGame);
                            }
                        }

                        gameList = new GameList(ListCategoryType.Favorites.ToString(), favoritesList);
                        gameList.ListCategoryType = ListCategoryType.Favorites;

                    }
                }
                */
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
                PluginHelper.DataManager.Save(false);
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
                CallGameChangeFunction();
                return true;
            }

            if (IsDisplayingMoreInfo)
            {
                IsDisplayingMoreInfo = false;
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