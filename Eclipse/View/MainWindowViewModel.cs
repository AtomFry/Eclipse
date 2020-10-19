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

namespace Eclipse.View
{
    public delegate void FeatureChangeFunction();
    public delegate void AnimateGameChangeFunction();
    public delegate void LoadImagesFunction();
    public delegate void IncrementLoadingProgressFunction();

    public static class Extensions
    {
        public static void AddGame(this Dictionary<string, List<GameMatch>> dictionary, string key, GameMatch gameMatch)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, new List<GameMatch>());
            }

            if (!dictionary[key].Contains(gameMatch))
            {
                dictionary[key].Add(gameMatch);
            }
        }

    }

    class MainWindowViewModel : INotifyPropertyChanged
    {
        private List<Option> listCategories;
        private ListCycle<GameList> listCycle;
        public List<GameListSet> GameListSets { get; set; }

        private List<GameList> TempGameLists { get; set; }
        private SpeechRecognitionEngine Recognizer { get; set; }

        private Dictionary<string, List<GameMatch>> GameTitlePhrases;
        private Dictionary<string, List<GameMatch>> GenreGameDictionary;
        private Dictionary<string, List<GameMatch>> PublisherGameDictionary;
        private Dictionary<string, List<GameMatch>> DeveloperGameDictionary;
        private Dictionary<string, List<GameMatch>> SeriesGameDictionary;
        private Dictionary<string, List<GameMatch>> PlayModeGameDictionary;
        
        // store platform bezels and default bezels in dictionary for easy lookup
        // this is probably terrible design but the problem is I won't know the video orientation in the GameMatch, I get that once the MediaElement is loaded over in the view
        // TODO: add game title (or id) here to take care of all game bezels too - currently game specific bezels are loaded up in the GameMatch
        // Key: <BezelType, BezelOrientation, Platform> 
        private Dictionary<Tuple<BezelType, BezelOrientation, string>, Uri> BezelDictionary;

        public FeatureChangeFunction FeatureChangeFunction { get; set; }
        public AnimateGameChangeFunction GameChangeFunction{ get; set; }
        public LoadImagesFunction LoadImagesFunction { get; set; }

        private List<IGame> allGames;
        public List<IGame> AllGames
        {
            get { return allGames; }
            set
            {
                if(allGames != value)
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
                if(isPickingCategory != value)
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
                if(isDisplayingFeature != value)
                {
                    isDisplayingFeature = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayingFeature"));
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

        private string errorMessage;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                if(errorMessage != value)
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
                if(loadingMessage != value)
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


        private GameListSet currentGameListSet;
        public GameListSet CurrentGameListSet
        {
            get { return currentGameListSet; }
            set
            {
                if(currentGameListSet != value)
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
                if(optionList != value)
                {
                    optionList = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OptionList"));
                }
            }
        }

        private void SetupCategoryList()
        {
            listCategories = new List<Option>();
            listCategories.Add(new Option { Name = "Platform", ListCategoryType=ListCategoryType.Platform, SortOrder=1, ShortDescription="Platform", LongDescription="Platform" });
            listCategories.Add(new Option { Name = "Genre", ListCategoryType = ListCategoryType.Genre, SortOrder=2, ShortDescription="Genre", LongDescription="Genre" });
            listCategories.Add(new Option { Name = "Series", ListCategoryType = ListCategoryType.Series, SortOrder=3, ShortDescription="Series", LongDescription="Series" });
            listCategories.Add(new Option { Name = "Playlist", ListCategoryType = ListCategoryType.Playlist, SortOrder=4, ShortDescription="Playlist", LongDescription="Playlist" });
            listCategories.Add(new Option { Name = "Voice search", ListCategoryType = ListCategoryType.VoiceSearch, SortOrder=5, ShortDescription="Voice", LongDescription="Voice search" });
            listCategories.Add(new Option { Name = "Play mode", ListCategoryType = ListCategoryType.PlayMode, SortOrder=6, ShortDescription="Mode", LongDescription="Play mode" });
            listCategories.Add(new Option { Name = "Developer", ListCategoryType = ListCategoryType.Developer, SortOrder=7, ShortDescription="Dev", LongDescription="Developer" });
            listCategories.Add(new Option { Name = "Publisher", ListCategoryType = ListCategoryType.Publisher, SortOrder=8, ShortDescription="Pub", LongDescription="Publisher" });
            listCategories.Add(new Option { Name = "Year", ListCategoryType = ListCategoryType.ReleaseYear, SortOrder=9, ShortDescription="Year", LongDescription="Release year" });
            listCategories.Add(new Option { Name = "Random", ListCategoryType = ListCategoryType.RandomGame, SortOrder=10, ShortDescription="Random", LongDescription="Random game" });
            OptionList = new OptionList(listCategories);
        }

        // todo: test falling back to platform bezels if game bezel is not there
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

                Helpers.Log($"{platformHorizontalBezelPath}");

                if(File.Exists(platformHorizontalBezelPath)) BezelDictionary.Add(platformHorizontalBezelKey, new Uri(platformHorizontalBezelPath));
                if(File.Exists(platformVerticalBezelPath)) BezelDictionary.Add(platformVerticalBezelKey, new Uri(platformVerticalBezelPath));
            }
        }

        // todo: test falling back to default bezels if game bezel and platform bezel is not there
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



        private void GetGamesByPlatform()
        {
            List<GameList> listOfPlatformGames = new List<GameList>();

            List<IPlatform> platforms = new List<IPlatform>(PluginHelper.DataManager.GetAllPlatforms());
            var orderedPlatforms = platforms.OrderBy(f => f.ReleaseDate);

            foreach (var platform in orderedPlatforms)
            {
                var platformGames = from game in AllGames
                                    where game.Platform.Equals(platform.Name)
                                    select new GameMatch(game, TitleMatchType.None);

                var orderedGames = platformGames.OrderBy(s => s.Game.SortTitleOrTitle);
                GameList gameList = new GameList(platform.Name, new List<GameMatch>(orderedGames));
                
                listOfPlatformGames.Add(gameList);
            }
            GameListSets.Add(new GameListSet { GameLists = listOfPlatformGames, ListCategoryType = ListCategoryType.Platform });
        }

        private void GetGamesByYear()
        {
            List <GameList> listOfYearGames = new List<GameList>();
            var gamesByYear = AllGames.GroupBy(game => game.ReleaseDate?.Year);

            foreach (var yearGameGroup in gamesByYear)
            {
                string year = yearGameGroup.Key?.ToString();

                var gameMatchQuery = from game in yearGameGroup
                                     select new GameMatch(game, TitleMatchType.None);

                var orderedGames = gameMatchQuery.OrderBy(game => game.Game.SortTitleOrTitle).ToList();
                GameList gameList = new GameList(year, orderedGames);
                listOfYearGames.Add(gameList);
            }
            GameListSets.Add(new GameListSet { GameLists = listOfYearGames.OrderBy(list => list.ListDescription).ToList(), ListCategoryType = ListCategoryType.ReleaseYear});
        }

        private void GetGamesByDeveloper()
        {
            List<GameList> listOfDeveloperGames = new List<GameList>();
            foreach(var developerGroup in DeveloperGameDictionary)
            {
                var orderedGames = developerGroup.Value.OrderBy(game => game.Game.SortTitleOrTitle).ToList();
                GameList gameList = new GameList(developerGroup.Key, orderedGames);
                listOfDeveloperGames.Add(gameList);
            }
            GameListSets.Add(new GameListSet { GameLists = listOfDeveloperGames.OrderBy(list => list.ListDescription).ToList(), ListCategoryType = ListCategoryType.Developer });
        }

        private void GetGamesByPlayMode()
        {
            List<GameList> listOfPlayModeGames = new List<GameList>();
            foreach (var playModeGroup in PlayModeGameDictionary)
            {
                var orderedGames = playModeGroup.Value.OrderBy(game => game.Game.SortTitleOrTitle).ToList();
                GameList gameList = new GameList(playModeGroup.Key, orderedGames);
                listOfPlayModeGames.Add(gameList);
            }
            GameListSets.Add(new GameListSet { GameLists = listOfPlayModeGames.OrderBy(list => list.ListDescription).ToList(), ListCategoryType = ListCategoryType.PlayMode });
        }

        private void GetGamesByPlaylist()
        {
            IPlaylist[] allPlaylists = PluginHelper.DataManager.GetAllPlaylists();
            List<GameList> listOfPlayListGames = new List<GameList>();

            foreach(IPlaylist playlist in allPlaylists)
            {
                if (playlist.HasGames(false, false))
                {
                    IGame[] playlistGames = playlist.GetAllGames(true);
                    var orderedPlatformGames = playlistGames.OrderBy(game => game.SortTitleOrTitle);
                    var query = from game in orderedPlatformGames select new GameMatch(game, TitleMatchType.None);

                    GameList gameList = new GameList(playlist.SortTitleOrTitle, query.ToList());
                    listOfPlayListGames.Add(gameList);
                }
            }
            GameListSets.Add(new GameListSet { GameLists = listOfPlayListGames, ListCategoryType = ListCategoryType.Playlist });
        }

        private void GetGamesByPublisher()
        {
            List<GameList> listOfPublisherGames = new List<GameList>();
            foreach(var publisherGroup in PublisherGameDictionary)
            {
                var orderedGames = publisherGroup.Value.OrderBy(game => game.Game.SortTitleOrTitle).ToList();
                GameList gameList = new GameList(publisherGroup.Key, orderedGames);
                listOfPublisherGames.Add(gameList);
            }
            GameListSets.Add(new GameListSet { GameLists = listOfPublisherGames.OrderBy(list => list.ListDescription).ToList(), ListCategoryType = ListCategoryType.Publisher });
        }

        private void GetGamesByGenre()
        {
            List<GameList> listOfGenreGames = new List<GameList>();
            foreach (var genreGroup in GenreGameDictionary)
            {
                var orderedGames = genreGroup.Value.OrderBy(game => game.Game.SortTitleOrTitle).ToList();
                GameList gameList = new GameList(genreGroup.Key, orderedGames);
                listOfGenreGames.Add(gameList);
            }
            GameListSets.Add(new GameListSet { GameLists = listOfGenreGames.OrderBy(list => list.ListDescription).ToList(), ListCategoryType = ListCategoryType.Genre });
        }

        private void GetGamesBySeries()
        {
            List<GameList> listOfSeriesGames = new List<GameList>();
            foreach (var seriesGroup in SeriesGameDictionary)
            {
                if (seriesGroup.Value?.Count != null && seriesGroup.Value?.Count > 1)
                {
                    var orderedGames = seriesGroup.Value.OrderBy(game => game.Game.SortTitleOrTitle).ToList();
                    GameList gameList = new GameList(seriesGroup.Key, orderedGames);
                    listOfSeriesGames.Add(gameList);
                }
            }
            GameListSets.Add(new GameListSet { GameLists = listOfSeriesGames.OrderBy(list => list.ListDescription).ToList(), ListCategoryType = ListCategoryType.Series });
        }

        public MainWindowViewModel()
        {
            IsInitializing = true;
            DeveloperGameDictionary = new Dictionary<string, List<GameMatch>>(StringComparer.InvariantCultureIgnoreCase);
            PublisherGameDictionary = new Dictionary<string, List<GameMatch>>(StringComparer.InvariantCultureIgnoreCase);
            GenreGameDictionary = new Dictionary<string, List<GameMatch>>(StringComparer.InvariantCultureIgnoreCase);
            GameTitlePhrases = new Dictionary<string, List<GameMatch>>(StringComparer.InvariantCultureIgnoreCase);
            SeriesGameDictionary = new Dictionary<string, List<GameMatch>>(StringComparer.InvariantCultureIgnoreCase);
            PlayModeGameDictionary = new Dictionary<string, List<GameMatch>>(StringComparer.InvariantCultureIgnoreCase);
            BezelDictionary = new Dictionary<Tuple<BezelType, BezelOrientation, string>, Uri>();

            // get all the games from the launchbox install
            AllGames = DataService.GetGames();
        }

        public Uri GetDefaultBezel(BezelType bezelType, BezelOrientation bezelOrientation, string platformName)
        {
            Helpers.Log($"GetDefaultBezel: {bezelType} - {bezelOrientation} - {platformName}");

            Uri bezelUri = GetBezel(bezelType, bezelOrientation, platformName);
            if(bezelUri != null)
            {
                Helpers.Log("Found platform bezel");

                return bezelUri;
            }

            // try the default bezel
            if(bezelType != BezelType.Default)
            {
                Helpers.Log("Platform bezel not found - checking for default bezel");
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

        void Initialization_LoadData(object sender, DoWorkEventArgs e)
        {
            try
            {
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
                foreach(FileInfo fileInfo in platformLogosToProcess)
                {
                    InitializationGameCount += 1;
                    ImageScaler.CropImage(fileInfo);
                }

                // crop game clear logos 
                foreach(FileInfo fileInfo in gameClearLogosToProcess)
                {
                    InitializationGameCount += 1;
                    ImageScaler.CropImage(fileInfo);
                }

                LoadingMessage = null;

                // load up game dictionaries to prepare lists and voice recognition
                foreach (IGame game in AllGames)
                {
                    InitializationGameCount += 1;

                    // create a dictionary of play modes and game matches
                    foreach (string playMode in game.PlayModes)
                    {
                        PlayModeGameDictionary.AddGame(playMode, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of Genres and game matches
                    foreach (string genre in game.Genres)
                    {
                        GenreGameDictionary.AddGame(genre, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of publishers and game matches
                    foreach (string publisher in game.Publishers)
                    {
                        PublisherGameDictionary.AddGame(publisher, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of developers and game matches
                    foreach (string developer in game.Developers)
                    {
                        DeveloperGameDictionary.AddGame(developer, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of series and game matches
                    foreach (string series in game.SeriesValues)
                    {
                        SeriesGameDictionary.AddGame(series, new GameMatch(game, TitleMatchType.None));
                    }

                    GameTitleGrammarBuilder gameTitleGrammarBuilder = new GameTitleGrammarBuilder(game);
                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Title))
                    {
                        AddGameToVoiceDictionary(gameTitleGrammarBuilder.Title, new GameMatch(game, TitleMatchType.FullTitleMatch, gameTitleGrammarBuilder.Title));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.MainTitle))
                    {
                        AddGameToVoiceDictionary(gameTitleGrammarBuilder.MainTitle, new GameMatch(game, TitleMatchType.MainTitleMatch, gameTitleGrammarBuilder.Title));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Subtitle))
                    {
                        AddGameToVoiceDictionary(gameTitleGrammarBuilder.Subtitle, new GameMatch(game, TitleMatchType.SubtitleMatch, gameTitleGrammarBuilder.Title));
                    }

                    for (int i = 0; i < gameTitleGrammarBuilder.TitleWords.Count; i++)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int j = i; j < gameTitleGrammarBuilder.TitleWords.Count; j++)
                        {
                            sb.Append($"{gameTitleGrammarBuilder.TitleWords[j]} ");
                            if (!GameTitleGrammarBuilder.IsNoiseWord(sb.ToString().Trim()))
                            {
                                AddGameToVoiceDictionary(sb.ToString().Trim(), new GameMatch(game, TitleMatchType.FullTitleContains, gameTitleGrammarBuilder.Title));
                            }
                        }
                    }
                }

                // todo: fix create voice recognizer for 11.3 and later
                CreateRecognizer();

                // create the list of options
                SetupCategoryList();

                // prepare lists of games by different categories
                GameListSets = new List<GameListSet>();
                GetGamesByPlatform();
                GetGamesByYear();
                GetGamesByGenre();
                GetGamesByPublisher();
                GetGamesByDeveloper();
                GetGamesBySeries();
                GetGamesByPlayMode();
                GetGamesByPlaylist();

                // flag initialization complete - display results
                IsInitializing = false;
                IsDisplayingResults = true;

                // default to display games by platform
                ResetGameLists(ListCategoryType.Platform);

                // start off with a random game
                DoRandomGame();
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
                List<string> titleElements = new List<string>(GameTitlePhrases.Keys);

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
            catch(Exception ex)
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
                foreach(var gameList in distinctGameLists)
                {
                    // get the list of matching games for the phrase from the GameTitlePhrases dictionary 
                    List<GameMatch> matches;
                    if(GameTitlePhrases.TryGetValue(gameList.ListDescription, out matches))
                    {
                        // override the match percentages for voice recognition 
                        foreach(var game in matches)
                        {
                            game.SetupVoiceMatchPercentage(gameList.Confidence, gameList.ListDescription);
                        }

                        gameList.MatchingGames = matches.OrderByDescending(match => match.matchPercentage).ToList();
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



        private void AddGameToVoiceDictionary(string phrase, GameMatch gameMatch)
        {
            if (GameTitleGrammarBuilder.IsNoiseWord(phrase))
            {
                return;
            }

            // add the phrase if it's not in the dictionary
            if (!GameTitlePhrases.ContainsKey(phrase))
            {
                GameTitlePhrases.Add(phrase, new List<GameMatch>());
            }

            // add the game if it's not already in the collection of matching games
            if (!GameTitlePhrases[phrase].Contains(gameMatch))
            {
                GameTitlePhrases[phrase].Add(gameMatch);
            }
        }

        public void DoRecognize()
        {
            if (IsRecognizing)
                return;

            if (IsInitializing)
                return;

            IsRecognizing = true;

            // TODO: look into leaving the IsDisplayingResults true and overlaying the recognition on top with progress bar to indicate how long it's listening
            // IsDisplayingResults = false;
            IsPickingCategory = false;
            IsDisplayingError = false;

            // reset the results and the temporary results
            TempGameLists = new List<GameList>();

            // kick off voice recognition 
            Recognizer.RecognizeAsync(RecognizeMode.Single);
        }

        private void RefreshGameLists()
        {
            if(listCycle?.GenericList?.Count == 0)
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
            if(GameChangeFunction != null)
            {
                GameChangeFunction();
            }
        }

        private void CallFeatureChangeFunction()
        {
            if(FeatureChangeFunction != null)
            {
                FeatureChangeFunction();
            }
        }

        private void CallLoadImageFunction()
        {
            if(LoadImagesFunction != null)
            {
                LoadImagesFunction();
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
                IsDisplayingError = false;


                if (isPickingCategory)
                {
                    OptionList.CycleBackward();
                    return;
                }

                if (isDisplayingResults)
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

                    if(IsDisplayingFeature)
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
                IsDisplayingError = false;


                if (isPickingCategory)
                {
                    OptionList.CycleForward();
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
                // stop displaying error
                IsDisplayingError = false;

                // if picking category and going left then close the category and keep going left
                if (IsPickingCategory)
                {
                    IsPickingCategory = false;
                    CurrentGameList.CycleBackward();
                    CallGameChangeFunction();
                    return;
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

                if (IsDisplayingFeature)
                {
                    // display category options
                    IsPickingCategory = true;
                    return;
                }
            }
            catch(Exception ex)
            {
                Helpers.LogException(ex, "DoLeft");
            }
        }

        public void DoRight(bool held)
        {
            try
            {
                // stop displaying error
                IsDisplayingError = false;

                // if picking category, going right closes category selection
                if (IsPickingCategory)
                {
                    IsPickingCategory = false;
                    CallGameChangeFunction();
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
            catch(Exception ex)
            {
                Helpers.LogException(ex, "DoRight");
            }
        }

        public void DoPageUp()
        {
            // stop displaying error
            IsDisplayingError = false;


            DoRandomGame();
        }

        public void DoPageDown()
        {
            // stop displaying error
            IsDisplayingError = false;

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
                CallLoadImageFunction();
                CallGameChangeFunction();
            }
        }

        private static readonly Random random = new Random();
        public void DoRandomGame()
        {
            // get a game index from the current list set
            int randomIndex = random.Next(0, CurrentGameListSet.TotalGameCount);
            
            // find the index of which list it's in 
            for(int listIndex = 0; listIndex < CurrentGameListSet.GameLists.Count; listIndex++)
            {
                GameList gameList = CurrentGameListSet.GameLists[listIndex];
                
                if(gameList.ListSetStartIndex <= randomIndex && gameList.ListSetEndIndex >= randomIndex)
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

        public void DoEnter()
        {
            // stop displaying error
            IsDisplayingError = false;

            if (isPickingCategory)
            {
                IsPickingCategory = false;

                Option option = OptionList.Option0;
                switch (option.ListCategoryType)
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

            if ((isDisplayingFeature) || (isDisplayingResults))
            {
                // start the current game
                IGame currentGame = CurrentGameList?.Game1?.Game;
                if (currentGame != null)
                {
                    PluginHelper.BigBoxMainViewModel.PlayGame(currentGame, null, null, null);
                }
                return;
            }
        }

        public bool DoEscape()
        {           
            // stop displaying error if it was displaying
            IsDisplayingError = false;

            // let bigbox handle escape and go to the bigbox menu
            if(IsPickingCategory)
            {
                return false;
            }

            // if displaying results - open the settings 
            if(IsDisplayingResults)
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

        public event PropertyChangedEventHandler PropertyChanged = delegate {};
    }
}