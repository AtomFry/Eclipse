using BigBoxNetflixUI.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using System.Data;
using BigBoxNetflixUI.Models;
using System.Speech.Recognition;
using System.Windows.Media.Imaging;

namespace BigBoxNetflixUI.View
{
    public delegate void AnimateGameChangeFunction();

    class MainWindowViewModel : INotifyPropertyChanged
    {
        private List<Option> listCategories;

        private ListCycle<GameList> listCycle;

        private List<GameList> TempGameLists { get; set; }
        private SpeechRecognitionEngine Recognizer { get; set; }

        private Dictionary<string, List<GameMatch>> GameTitlePhrases;
        private Dictionary<string, List<GameMatch>> GenreGameDictionary;
        private Dictionary<string, List<GameMatch>> PublisherGameDictionary;
        private Dictionary<string, List<GameMatch>> DeveloperGameDictionary;
        private Dictionary<string, List<GameMatch>> SeriesGameDictionary;
        private Dictionary<string, List<GameMatch>> PlayModeGameDictionary;

        public AnimateGameChangeFunction GameChangeFunction{ get; set; }

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


        private List<GameList> gameLists;
        public List<GameList> GameLists
        {
            get { return gameLists; }
            set
            {
                if(gameLists != value)
                {
                    gameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("GameLists"));
                }
            }
        }

        private List<GameList> platformGameLists;
        public List<GameList> PlatformGameLists
        {
            get { return platformGameLists; }
            set
            {
                if (platformGameLists != value)
                {
                    platformGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PlatformGameLists"));
                }
            }
        }

        private List<GameList> genreGameLists;
        public List<GameList> GenreGameLists
        {
            get { return genreGameLists; }
            set
            {
                if (genreGameLists != value)
                {
                    genreGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("GenreGameLists"));
                }
            }
        }

        private List<GameList> seriesGameLists;
        public List<GameList> SeriesGameLists
        {
            get { return seriesGameLists; }
            set
            {
                if (seriesGameLists != value)
                {
                    seriesGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("SeriesGameLists"));
                }
            }
        }

        private List<GameList> voiceSearchGameLists;
        public List<GameList> VoiceSearchGameLists
        {
            get { return voiceSearchGameLists; }
            set
            {
                if (voiceSearchGameLists != value)
                {
                    voiceSearchGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("VoiceSearchGameLists"));
                }
            }
        }

        private List<GameList> releaseYearGameLists;
        public List<GameList> ReleaseYearGameLists
        {
            get { return releaseYearGameLists; }
            set
            {
                if (releaseYearGameLists != value)
                {
                    releaseYearGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ReleaseYearGameLists"));
                }
            }
        }

        private List<GameList> playlistGameLists;
        public List<GameList> PlaylistGameLists
        {
            get { return playlistGameLists; }
            set
            {
                if (playlistGameLists != value)
                {
                    playlistGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PlaylistGameLists"));
                }
            }
        }

        private List<GameList> playModeGameLists;
        public List<GameList> PlayModeGameLists
        {
            get { return playModeGameLists; }
            set
            {
                if (playModeGameLists != value)
                {
                    playModeGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PlayModeGameLists"));
                }
            }
        }

        private List<GameList> developerGameLists;
        public List<GameList> DeveloperGameLists
        {
            get { return developerGameLists; }
            set
            {
                if (developerGameLists != value)
                {
                    developerGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("DeveloperGameLists"));
                }
            }
        }

        private List<GameList> publisherGameLists;
        public List<GameList> PublisherGameLists
        {
            get { return publisherGameLists; }
            set
            {
                if (publisherGameLists != value)
                {
                    publisherGameLists = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PublisherGameLists"));
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
            PlatformGameLists = listOfPlatformGames;
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
            ReleaseYearGameLists = listOfYearGames.OrderBy(list => list.ListDescription).ToList();
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
            DeveloperGameLists = listOfDeveloperGames.OrderBy(list => list.ListDescription).ToList();
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
            PlayModeGameLists = listOfPlayModeGames.OrderBy(list => list.ListDescription).ToList();
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
            PlaylistGameLists = listOfPlayListGames;
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
            PublisherGameLists = listOfPublisherGames.OrderBy(list => list.ListDescription).ToList();
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
            GenreGameLists = listOfGenreGames.OrderBy(list => list.ListDescription).ToList();
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
            SeriesGameLists = listOfSeriesGames.OrderBy(list => list.ListDescription).ToList();
        }

        public MainWindowViewModel()
        {
            IsInitializing = true;
            InitializeData();
        }

        private void InitializeData()
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
                IsInitializing = true;

                DeveloperGameDictionary = new Dictionary<string, List<GameMatch>>();
                PublisherGameDictionary = new Dictionary<string, List<GameMatch>>();
                GenreGameDictionary = new Dictionary<string, List<GameMatch>>();
                GameTitlePhrases = new Dictionary<string, List<GameMatch>>();
                SeriesGameDictionary = new Dictionary<string, List<GameMatch>>();
                PlayModeGameDictionary = new Dictionary<string, List<GameMatch>>();

                AllGames = DataService.GetGames();
                TotalGameCount = AllGames?.Count ?? 0;

                InitializationGameCount = 0;
                foreach (IGame game in AllGames)
                {
                    InitializationGameCount += 1;

                    // create a dictionary of play modes and game matches
                    foreach (string playMode in game.PlayModes)
                    {
                        AddGameToPlayModeDictionary(playMode, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of Genres and game matches
                    foreach (string genre in game.Genres)
                    {
                        AddGameToGenreDictionary(genre, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of publishers and game matches
                    foreach (string publisher in game.Publishers)
                    {
                        AddGameToPublisherDictionary(publisher, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of developers and game matches
                    foreach (string developer in game.Developers)
                    {
                        AddGameToDeveloperDictionary(developer, new GameMatch(game, TitleMatchType.None));
                    }

                    // create a dictionary of series and game matches
                    foreach (string series in game.SeriesValues)
                    {
                        AddGameToSeriesDictionary(series, new GameMatch(game, TitleMatchType.None));
                    }

                    GameTitleGrammarBuilder gameTitleGrammarBuilder = new GameTitleGrammarBuilder(game);

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Title))
                    {
                        AddGameToVoiceDictionary(gameTitleGrammarBuilder.Title, new GameMatch(game, TitleMatchType.FullTitleMatch));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.MainTitle))
                    {
                        AddGameToVoiceDictionary(gameTitleGrammarBuilder.MainTitle, new GameMatch(game, TitleMatchType.MainTitleMatch));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Subtitle))
                    {
                        AddGameToVoiceDictionary(gameTitleGrammarBuilder.Subtitle, new GameMatch(game, TitleMatchType.SubtitleMatch));
                    }

                    for (int i = 0; i < gameTitleGrammarBuilder.TitleWords.Count; i++)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int j = i; j < gameTitleGrammarBuilder.TitleWords.Count; j++)
                        {
                            sb.Append($"{gameTitleGrammarBuilder.TitleWords[j]} ");
                            if (!GameTitleGrammarBuilder.IsNoiseWord(sb.ToString().Trim()))
                            {
                                AddGameToVoiceDictionary(sb.ToString().Trim(), new GameMatch(game, TitleMatchType.FullTitleContains));
                            }
                        }
                    }
                }

                // todo: fix create voice recognizer for 11.3 and later
                CreateRecognizer();

                // create the list of options
                SetupCategoryList();

                // prepare lists of games by different categories
                GetGamesByPlatform();
                GetGamesByYear();
                GetGamesByGenre();
                GetGamesByPublisher();
                GetGamesByDeveloper();
                GetGamesBySeries();
                GetGamesByPlayMode();
                GetGamesByPlaylist();

                // default to display games by platform
                ResetGameLists(PlatformGameLists);

                // flag initialization complete - display results
                IsInitializing = false;
                IsDisplayingResults = true;
            }
            catch(Exception ex)
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

            if (TempGameLists?.Count() > 0)
            {
                // in case the same phrase was recognized multiple times, group by phrase and keep only the max confidence
                var distinctGameLists = TempGameLists
                    .GroupBy(s => s.ListDescription)
                    .Select(s => new GameList { ListDescription = s.Key, Confidence = s.Max(m => m.Confidence) }).ToList();

                foreach(var gameList in distinctGameLists)
                {
                    List<GameMatch> matches;
                    if(GameTitlePhrases.TryGetValue(gameList.ListDescription, out matches))
                    {
                        gameList.MatchingGames = matches;
                        voiceRecognitionResults.Add(gameList);
                    }
                }
            }

            VoiceSearchGameLists = voiceRecognitionResults;
            ResetGameLists(VoiceSearchGameLists);
            IsDisplayingResults = true;
        }


        private void AddGameToGenreDictionary(string genre, GameMatch gameMatch)
        {
            if(string.IsNullOrWhiteSpace(genre))
            {
                return;
            }

            if(!GenreGameDictionary.ContainsKey(genre))
            {
                GenreGameDictionary.Add(genre, new List<GameMatch>());
            }

            if(!GenreGameDictionary[genre].Contains(gameMatch))
            {
                GenreGameDictionary[genre].Add(gameMatch);
            }
        }

        private void AddGameToPlayModeDictionary(string playMode, GameMatch gameMatch)
        {
            if (string.IsNullOrWhiteSpace(playMode))
            {
                return;
            }

            if (!PlayModeGameDictionary.ContainsKey(playMode))
            {
                PlayModeGameDictionary.Add(playMode, new List<GameMatch>());
            }

            if (!PlayModeGameDictionary[playMode].Contains(gameMatch))
            {
                PlayModeGameDictionary[playMode].Add(gameMatch);
            }
        }

        private void AddGameToDeveloperDictionary(string developer, GameMatch gameMatch)
        {
            if (string.IsNullOrWhiteSpace(developer))
            {
                return;
            }

            if (!DeveloperGameDictionary.ContainsKey(developer))
            {
                DeveloperGameDictionary.Add(developer, new List<GameMatch>());
            }

            if (!DeveloperGameDictionary[developer].Contains(gameMatch))
            {
                DeveloperGameDictionary[developer].Add(gameMatch);
            }
        }

        private void AddGameToPublisherDictionary(string publisher, GameMatch gameMatch)
        {
            if (string.IsNullOrWhiteSpace(publisher))
            {
                return;
            }

            if (!PublisherGameDictionary.ContainsKey(publisher))
            {
                PublisherGameDictionary.Add(publisher, new List<GameMatch>());
            }

            if (!PublisherGameDictionary[publisher].Contains(gameMatch))
            {
                PublisherGameDictionary[publisher].Add(gameMatch);
            }
        }

        private void AddGameToSeriesDictionary(string series, GameMatch gameMatch)
        {
            if (string.IsNullOrWhiteSpace(series))
            {
                return;
            }

            if (!SeriesGameDictionary.ContainsKey(series))
            {
                SeriesGameDictionary.Add(series, new List<GameMatch>());
            }

            if (!SeriesGameDictionary[series].Contains(gameMatch))
            {
                SeriesGameDictionary[series].Add(gameMatch);
            }
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
            IsDisplayingResults = false;
            IsDisplayingError = false;

            // reset the results and the temporary results
            TempGameLists = new List<GameList>();

            // kick off voice recognition 
            Recognizer.RecognizeAsync(RecognizeMode.Single);
        }

        private void RefreshGameLists()
        {
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
            if (isPickingCategory)
            {
                OptionList.CycleBackward();
                return;
            }

            if (isDisplayingResults)
            {
                // todo: if displaying first list - change to featured game

                // cycle to prior list
                CycleListBackward();
                return;
            }
        }

        public void DoDown(bool held)
        {
            if (isPickingCategory)
            {
                OptionList.CycleForward();
                return;
            }

            if (isDisplayingFeature)
            {
                // todo: change to displaying first result
                return;
            }

            if (isDisplayingResults)
            {
                // cycle to next list
                CycleListForward();
                return;
            }
        }

        public void DoLeft(bool held)
        {
            // if picking category and going left then close the category and keep going left
            if(IsPickingCategory)
            {
                IsPickingCategory = false;
                CurrentGameList.CycleBackward();
                CallGameChangeFunction();
                return;
            }

            // if current game is the first game then going left displays the category options
            if(IsDisplayingResults && CurrentGameList.CurrentGameIndex == 0)
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

        public void DoRight(bool held)
        {
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

        public void DoPageUp()
        {
            // do voice recognition
            DoRecognize();
        }

        public void DoPageDown()
        {
            // do voice recognition
            DoRecognize();
        }

        private void ResetGameLists(List<GameList> _gamelists)
        {
            GameLists = _gamelists;
            listCycle = new ListCycle<GameList>(GameLists, 2);
            RefreshGameLists();
        }

        public void DoEnter()
        {
            if(isPickingCategory)
            {
                Option option = OptionList.Option0;
                switch (option.ListCategoryType)
                {
                    case ListCategoryType.VoiceSearch:
                        DoRecognize();
                        break;

                    case ListCategoryType.RandomGame:
                        // todo: implement random game
                        break;

                    case ListCategoryType.ReleaseYear:
                        ResetGameLists(ReleaseYearGameLists);
                        break;

                    case ListCategoryType.Platform:
                        ResetGameLists(PlatformGameLists);
                        break;

                    case ListCategoryType.Developer:
                        ResetGameLists(DeveloperGameLists);
                        break;

                    case ListCategoryType.Genre:
                        ResetGameLists(GenreGameLists);
                        break;

                    case ListCategoryType.Playlist:
                        ResetGameLists(PlaylistGameLists);
                        break;

                    case ListCategoryType.PlayMode:
                        ResetGameLists(PlayModeGameLists);
                        break;

                    case ListCategoryType.Publisher:
                        ResetGameLists(PublisherGameLists);
                        break;

                    case ListCategoryType.Series:
                        ResetGameLists(SeriesGameLists);
                        break;
                }

                IsPickingCategory = false;
                return;
            }

            if (isDisplayingFeature)
            {
                // todo: start featured game
                return;
            }

            if(isDisplayingResults)
            {
                // start the current game
                IGame currentGame = CurrentGameList?.Game1?.Game;
                if (currentGame != null)
                {
                    // todo: show or start game depending on settings?
                    PluginHelper.BigBoxMainViewModel.PlayGame(currentGame, null, null, null);
                }
                return;
            }
        }

        public void DoEscape()
        {
            // todo: TBD - maybe nothing - maybe go back to prior setting
        }

        private GameList currentGameList;
        public GameList CurrentGameList
        {
            get { return currentGameList; }
            set
            {
                if (currentGameList != value)
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

        public Uri VoiceRecognitionGif
        {
            get
            {
                return new Uri("pack://application:,,,/BigBoxNetflixUI;component/resources/VoiceRecognitionGif.gif");
            }
        }

        public Uri SettingsIconGrey
        {
            get
            {
                return new Uri("pack://application:,,,/BigBoxNetflixUI;component/resources/SettingsIcon_Grey.png");
            }
        }

        public Uri SettingsIconWhite
        {
            get
            {
                return new Uri("pack://application:,,,/BigBoxNetflixUI;component/resources/SettingsIcon_White.png");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
