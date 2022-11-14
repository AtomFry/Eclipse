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

namespace Eclipse.View
{
    public delegate void AnimateGameChangeFunction();
    public delegate void IncrementLoadingProgressFunction();
    public delegate void StopVideoAndAnimations();
    public delegate void UpdateRatingImage();

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ListCycle<GameList> listCycle;
        public List<GameListSet> GameListSets;
        public ConcurrentBag<GameMatch> gameBag;
        public ConcurrentBag<GameFiles> gameFilesBag;

        private bool isInitializing;
        private bool isPickingCategory;
        private bool isDisplayingFeature;
        private bool isDisplayingAttractMode;
        private bool isRecognizing;
        private bool isDisplayingResults;
        private bool isDisplayingMoreInfo;
        private bool isDisplayingError;
        private bool isDisplayingSearch;
        private bool isRatingGame;

        private GameDetailOption gameDetailOption;
        private string errorMessage;

        public EclipseStateContext EclipseStateContext { get; set; }

        public MainWindowViewModel()
        {
            IsInitializing = true;

            FeatureOption = FeatureGameOption.PlayGame;

            EclipseStateContext = new EclipseStateContext(this);
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

        public bool IsRecognizing
        {
            get => isRecognizing;
            set
            {
                if (isRecognizing != value)
                {
                    isRecognizing = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsRecognizing"));
                }
            }
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

        public GameDetailOption GameDetailOption
        {
            get => gameDetailOption;
            set
            {
                {
                    gameDetailOption = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("GameDetailOption"));
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
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentGameListSet"));
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
                    PropertyChanged(this, new PropertyChangedEventArgs("OptionList"));
                }
            }
        }

        private void GetGamesByListCategoryType(ListCategoryType listCategoryType)
        {
            List<GameList> listOfGameList = new List<GameList>();

            // remove any prior set of this type and then add these results to the set list category
            GameListSets.RemoveAll(set => set.ListCategoryType == listCategoryType);

            IEnumerable<CustomListDefinition> customListDefinitions = new CustomListDefinitionDataProvider().GetAllCustomListDefinitions();
            IEnumerable<CustomListDefinition> filteredCustomListDefinitions = from customListDefinition in customListDefinitions
                                                                              where customListDefinition.ListCategoryTypes.Contains(listCategoryType)
                                                                              select customListDefinition;

            int sortOrder = 0;
            foreach (CustomListDefinition customListDefinition in filteredCustomListDefinitions)
            {
                IQueryable<GameMatch> baseQuery = gameBag.Where(g => g.CategoryType == ListCategoryType.Platform).AsQueryable();

                if (customListDefinition.FilterExpressions.Any())
                {
                    foreach (FilterExpression filterExpression in customListDefinition.FilterExpressions)
                    {
                        baseQuery = baseQuery.ApplyDynamicFilter(filterExpression.GameFieldEnum.ToFieldName(), filterExpression.FilterFieldOperator, filterExpression.FilterFieldValue);
                    }
                }

                var orderedQuery = baseQuery.OrderBy(g => g.Game.SortTitleOrTitle);

                if (customListDefinition.SortExpressions.Any())
                {
                    bool first = true;
                    foreach (var sortExpression in customListDefinition.SortExpressions)
                    {
                        if (first)
                        {
                            first = false;
                            switch (sortExpression.SortDirection)
                            {
                                case SortDirection.Ascending:
                                    orderedQuery = baseQuery.OrderBy(sortExpression.GameFieldEnum.ToFieldName());
                                    break;
                                case SortDirection.Descending:
                                    orderedQuery = baseQuery.OrderByDescending(sortExpression.GameFieldEnum.ToFieldName());
                                    break;
                            }
                        }
                        else
                        {
                            switch (sortExpression.SortDirection)
                            {
                                case SortDirection.Ascending:
                                    orderedQuery = orderedQuery.ThenBy(sortExpression.GameFieldEnum.ToFieldName());
                                    break;
                                case SortDirection.Descending:
                                    orderedQuery = orderedQuery.ThenByDescending(sortExpression.GameFieldEnum.ToFieldName());
                                    break;
                            }
                        }
                    }
                }

                baseQuery = orderedQuery;
                if (customListDefinition.MaxGamesInList > 0)
                {
                    baseQuery = orderedQuery.Take(customListDefinition.MaxGamesInList).AsQueryable();
                }

                if (baseQuery.Any())
                {
                    listOfGameList.Add(new GameList(customListDefinition.Description, baseQuery.ToList(), sortOrder++));
                }
            }

            var gameQuery = from gameMatch in gameBag
                            where gameMatch.CategoryType == listCategoryType
                            group gameMatch by gameMatch.CategoryValue into gameGroup
                            select gameGroup;

            foreach (var gameGroup in gameQuery)
            {
                listOfGameList.Add(new GameList(gameGroup.Key, gameGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList()));
            }

            GameListSets.Add(new GameListSet
            {
                GameLists = listOfGameList.OrderBy(list => list.SortOrder)
                                            .ThenBy(list => list.ListDescription).ToList(),
                ListCategoryType = listCategoryType
            });
        }

        public async void SetupFiles(object sender, DoWorkEventArgs e)
        {
            int? GameFilesCount = gameFilesBag?.Count;
            int processedCount = 0;

            await Task.Run(async () =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                while (await SetupNextGameFiles())
                {
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
                        IEnumerable<GameMatch> currentListQuery = CurrentGameList.MatchingGames.Where(g => !g.GameFiles.IsSetup);
                        if (currentListQuery.Any())
                        {
                            GameMatch gameMatchCurrentList = currentListQuery.FirstOrDefault();
                            if (gameMatchCurrentList?.GameFiles != null)
                            {
                                moreGameFiles = true;
                                await gameMatchCurrentList.GameFiles.SetupFiles();

                                if (gameMatchCurrentList?.GameFiles?.Game?.Id == currentGameList?.Game1?.Game?.Id)
                                {
                                    CallGameChangeFunction();
                                }
                            }
                        }
                    }

                    // setup a game in the next list
                    if (NextGameList != null && NextGameList.MatchingGames != null)
                    {
                        IEnumerable<GameMatch> nextListQuery = NextGameList.MatchingGames.Where(g => !g.GameFiles.IsSetup);
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
                        IEnumerable<GameFiles> anyGameQuery = gameFilesBag.Where(gf => !gf.IsSetup);
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
            GetGamesByListCategoryType(ListCategoryType.Platform);
            GetGamesByListCategoryType(ListCategoryType.ReleaseYear);
            GetGamesByListCategoryType(ListCategoryType.Genre);
            GetGamesByListCategoryType(ListCategoryType.Publisher);
            GetGamesByListCategoryType(ListCategoryType.Developer);
            GetGamesByListCategoryType(ListCategoryType.Series);
            GetGamesByListCategoryType(ListCategoryType.PlayMode);
            GetGamesByListCategoryType(ListCategoryType.Playlist);
        }

        public void DoMoreLikeCurrentGame()
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

                        foreach (GameList gameList in seriesGameListQuery)
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
                if (genreGameListSet != null)
                {
                    foreach (string genre in currentGame?.Game?.Genres)
                    {
                        var genreGameListQuery = from genreGameList in genreGameListSet.GameLists
                                                 where genreGameList.ListDescription.Equals(genre, StringComparison.InvariantCultureIgnoreCase)
                                                 select genreGameList;

                        foreach (GameList gameList in genreGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }


                // get platform list 
                IEnumerable<GameListSet> platformGameListSetQuery = from gameListSet in GameListSets
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
                IEnumerable<GameListSet> developerGameListSetQuery = from gameListSet in GameListSets
                                                                     where gameListSet.ListCategoryType == ListCategoryType.Developer
                                                                     select gameListSet;

                GameListSet developerGameListSet = developerGameListSetQuery?.FirstOrDefault();
                if (developerGameListSet != null)
                {
                    foreach (string developer in currentGame?.Game?.Developers)
                    {
                        IEnumerable<GameList> developerGameListQuery = from developerGameList in developerGameListSet.GameLists
                                                                       where developerGameList.ListDescription.Equals(developer, StringComparison.InvariantCultureIgnoreCase)
                                                                       select developerGameList;

                        foreach (GameList gameList in developerGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Publisher list
                IEnumerable<GameListSet> publisherGameListSetQuery = from gameListSet in GameListSets
                                                                     where gameListSet.ListCategoryType == ListCategoryType.Publisher
                                                                     select gameListSet;

                GameListSet publisherGameListSet = publisherGameListSetQuery?.FirstOrDefault();
                if (publisherGameListSet != null)
                {
                    foreach (string publisher in currentGame?.Game?.Publishers)
                    {
                        IEnumerable<GameList> publisherGameListQuery = from publisherGameList in publisherGameListSet.GameLists
                                                                       where publisherGameList.ListDescription.Equals(publisher, StringComparison.InvariantCultureIgnoreCase)
                                                                       select publisherGameList;

                        foreach (GameList gameList in publisherGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Play mode list
                IEnumerable<GameListSet> playModeGameListSetQuery = from gameListSet in GameListSets
                                                                    where gameListSet.ListCategoryType == ListCategoryType.PlayMode
                                                                    select gameListSet;

                GameListSet playModeGameListSet = playModeGameListSetQuery?.FirstOrDefault();
                if (playModeGameListSet != null)
                {
                    foreach (string playMode in currentGame?.Game?.PlayModes)
                    {
                        IEnumerable<GameList> playModeGameListQuery = from playModeGameList in playModeGameListSet.GameLists
                                                                      where playModeGameList.ListDescription.Equals(playMode, StringComparison.InvariantCultureIgnoreCase)
                                                                      select playModeGameList;

                        foreach (GameList gameList in playModeGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Release year list
                IEnumerable<GameListSet> releaseYearGameListSetQuery = from gameListSet in GameListSets
                                                                       where gameListSet.ListCategoryType == ListCategoryType.ReleaseYear
                                                                       select gameListSet;

                GameListSet releaseYearGameListSet = releaseYearGameListSetQuery?.FirstOrDefault();
                if (releaseYearGameListSet != null)
                {
                    int? releaseYear = currentGame?.Game?.ReleaseDate?.Year;
                    if (releaseYear != null)
                    {
                        IEnumerable<GameList> releaseYearGameListQuery = from releaseYearGameList in releaseYearGameListSet.GameLists
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

        private GameMatch attractModeGame;
        public GameMatch AttractModeGame
        {
            get => attractModeGame;
            set
            {
                if (attractModeGame != value)
                {
                    attractModeGame = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("AttractModeGame"));
                }
            }
        }

        public void NextAttractModeGame()
        {
            int randomIndex = random.Next(gameBag.Count);
            AttractModeGame = gameBag.ElementAt(randomIndex);
        }

        private void RefreshGameLists()
        {
            if (listCycle?.GenericList?.Count == 0)
            {
                IsDisplayingError = true;
                ErrorMessage = "A problem occurred trying to refresh the list of games";
                return;
            }

            CurrentGameList = listCycle.GetItem(0);
            NextGameList = listCycle.GetItem(1);
            CallGameChangeFunction();
        }

        public void CallGameChangeFunction()
        {
            GameChangeFunction?.Invoke();
        }

        public void CallStopVideoAndAnimationsFunction()
        {
            StopVideoAndAnimationsFunction?.Invoke();
        }

        private void CallUpdateRatingImageFunction()
        {
            UpdateRatingImageFunction?.Invoke();
        }

        public void CycleListBackward()
        {
            listCycle.CycleBackward();
            RefreshGameLists();
        }

        public void CycleListForward()
        {
            listCycle.CycleForward();
            RefreshGameLists();
        }

        public bool DoUp(bool held)
        {
            return EclipseStateContext.OnUp(held);
        }

        public bool DoDown(bool held)
        {
            return EclipseStateContext.OnDown(held);
        }

        public bool DoLeft(bool held)
        {
            return EclipseStateContext.OnLeft(held);
        }

        public bool DoRight(bool held)
        {
            return EclipseStateContext.OnRight(held);
        }

        public bool DoPageUp()
        {
            return EclipseStateContext.OnPageUp();
        }

        public bool DoPageDown()
        {
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

        // start the current game
        public void PlayCurrentGame()
        {
            // todo: how can we make sure to update the images and game details for the selected game? - these can get out of sync if you select and start a game while the image is still fading in


            // get a handle on the current game 
            IGame currentGame = CurrentGameList?.Game1?.Game;
            if (currentGame != null)
            {
                currentGame.LastPlayedDate = DateTime.Now;

                // reset the lists so the updated history reflects - first save the current game details then reload the lists 
                SaveStateForGameListChange();
                ResetListsAfterChange();

                // stop everything in the UI
                CallStopVideoAndAnimationsFunction();

                // launch the game 
                PluginHelper.BigBoxMainViewModel.PlayGame(currentGame, null, null, null);
            }
            return;
        }

        // mark current game as a favorite
        public void FavoriteCurrentGame()
        {
            GameMatch currentGame = CurrentGameList?.Game1;
            if (currentGame != null)
            {
                currentGame.Favorite = !currentGame.Favorite;

                PluginHelper.DataManager.Save(false);

                IEnumerable<GameMatch> gameMatchQuery = from gameMatch in gameBag
                                                        where gameMatch.Game.Id == currentGame.Game.Id
                                                        select gameMatch;

                // flag the game as a favorite wherever it appears
                foreach (GameMatch gameMatch in gameMatchQuery)
                {
                    gameMatch.Favorite = currentGame.Favorite;
                }

                // save state so we can get back to the current game
                SaveStateForGameListChange();
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
        private int preChangeGameIndex;

        // call this when lists are about to change to save which list set, list, and game we were on so we can find our way back after rebuilding lists
        // this is needed when game lists are going to change (i.e. adding/removing favorites, adding/removing from history)
        private void SaveStateForGameListChange()
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
            if (gameListsChanged)
            {
                ResetListsAfterChange();
            }
        }

        // call this when lists have changed (i.e. game added/removed from favorites history list)
        // will try to find the game in the same list - if it can't (i.e. in favorites and game removed from favorites) then jumps to the next game
        private void ResetListsAfterChange()
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
                        DoRandomGame(gameList.ListSetStartIndex + gameIndex);
                        return;
                    }
                }

                // the game was not in the list so try the next game in the list 
                if (gameList?.MatchingGames?.Count() > preChangeGameIndex)
                {
                    DoRandomGame(gameList.ListSetStartIndex + preChangeGameIndex);
                    return;
                }

                // there was no next game so try a previous game in the list 
                if (gameList?.MatchingGames?.Count() > preChangeGameIndex - 1)
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

        public void RateCurrentGame(float changeAmount)
        {
            GameMatch currentGame = CurrentGameList?.Game1;
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

        public void SaveRatingCurrentGame()
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

        public bool DoEnter()
        {
            return EclipseStateContext.OnEnter();
        }

        public bool DoEscape()
        {
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
    }


    public static class CustomGameListServiceExtensionMethods
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "OrderBy");
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "OrderByDescending");
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "ThenBy");
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "ThenByDescending");
        }

        static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName)
        {
            string[] props = property.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            object result = typeof(Queryable)
                .GetMethods()
                .Single(method => method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetGenericArguments().Length == 2
                            && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), type)
                .Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }

        public static IQueryable<T> ApplyDynamicFilter<T>(this IQueryable<T> source, string property, FilterFieldOperator filterFieldOperator, object value)
        {
            string[] props = property.Split('.');
            Type type = typeof(T);

            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Expression left = expr;
            Expression constant = Expression.Constant(value);
            Expression right = Expression.Convert(constant, type);

            Expression whereExpression;
            switch (filterFieldOperator)
            {
                case FilterFieldOperator.Equal:
                    whereExpression = Expression.Equal(left, right);
                    break;

                case FilterFieldOperator.NotEqual:
                    whereExpression = Expression.NotEqual(left, right);
                    break;

                case FilterFieldOperator.GreaterThan:
                    whereExpression = Expression.GreaterThan(left, right);
                    break;

                case FilterFieldOperator.GreaterThanOrEqual:
                    whereExpression = Expression.GreaterThanOrEqual(left, right);
                    break;

                case FilterFieldOperator.LessThan:
                    whereExpression = Expression.LessThan(left, right);
                    break;

                case FilterFieldOperator.LessThanOrEqual:
                    whereExpression = Expression.LessThanOrEqual(left, right);
                    break;

                case FilterFieldOperator.IsNull:
                    right = Expression.Constant(null);
                    whereExpression = Expression.Equal(left, right);
                    break;

                case FilterFieldOperator.IsNotNull:
                    right = Expression.Constant(null);
                    whereExpression = Expression.NotEqual(left, right);
                    break;

                case FilterFieldOperator.Contains:
                    MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    whereExpression = Expression.Call(left, method, right);
                    break;

                default:
                    whereExpression = null;
                    break;
            }

            if (whereExpression == null)
            {
                return source;
            }

            var lambda = Expression.Lambda<Func<T, bool>>(whereExpression, arg).Compile();

            return source.Where(lambda).AsQueryable();
        }
    }
}