using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Eclipse.Models;
using Eclipse.Helpers;
using Eclipse.Service;
using Eclipse.View;
using Eclipse.State;
using System.Runtime.CompilerServices;

namespace Eclipse.Service
{
    public class GameListManagementService : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Reference to MainWindowViewModel for accessing game data and state
        private readonly MainWindowViewModel mainWindowViewModel;

        public GameListManagementService(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private void GetGamesByListCategoryType(ListCategoryType listCategoryType)
        {
            List<GameList> listOfGameList = new List<GameList>();

            // remove any prior set of this type and then add these results to the set list category
            mainWindowViewModel.GameListSets.RemoveAll(set => set.ListCategoryType == listCategoryType);

            IEnumerable<CustomListDefinition> customListDefinitions = new CustomListDefinitionDataProvider().GetAllCustomListDefinitions();
            IEnumerable<CustomListDefinition> filteredCustomListDefinitions = from customListDefinition in customListDefinitions
                                                                              where customListDefinition.ListCategoryTypes.Contains(listCategoryType)
                                                                              select customListDefinition;

            int sortOrder = 0;
            foreach (CustomListDefinition customListDefinition in filteredCustomListDefinitions)
            {
                IQueryable<GameMatch> baseQuery = mainWindowViewModel.gameBag.Where(g => g.CategoryType == ListCategoryType.Platform).AsQueryable();

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

            var gameQuery = from gameMatch in mainWindowViewModel.gameBag
                            where gameMatch.CategoryType == listCategoryType
                            group gameMatch by gameMatch.CategoryValue into gameGroup
                            select gameGroup;

            foreach (var gameGroup in gameQuery)
            {
                listOfGameList.Add(new GameList(gameGroup.Key, gameGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList()));
            }

            // include playlists in platforms if they are set to be included 
            if (listCategoryType == ListCategoryType.Platform)
            {
                Dictionary<string, bool> playlists = PlaylistGameService.Instance.Playlists;

                var playListQuery = from gameMatch in mainWindowViewModel.gameBag
                                    where gameMatch.CategoryType == ListCategoryType.Playlist
                                    group gameMatch by gameMatch.CategoryValue into gameGroup
                                    select gameGroup;

                foreach (var gameGroup in playListQuery)
                {
                    bool includeInPlaylists = false;
                    if (playlists.TryGetValue(gameGroup.Key, out includeInPlaylists))
                    {
                        if (includeInPlaylists)
                        {
                            listOfGameList.Add(new GameList(gameGroup.Key, gameGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList()));
                        }
                    }
                }
            }

            mainWindowViewModel.GameListSets.Add(new GameListSet
            {
                GameLists = listOfGameList.OrderBy(list => list.SortOrder)
                                            .ThenBy(list => list.ListDescription).ToList(),
                ListCategoryType = listCategoryType
            });
        }

        public void DoMoreLikeCurrentGame()
        {
            GameMatch currentGame = mainWindowViewModel.CurrentGameList?.Game1;
            if (currentGame != null)
            {
                List<GameList> moreLikeThisResults = new List<GameList>();

                // get lists for matching series
                var seriesGameListSetQuery = from gameListSet in mainWindowViewModel.GameListSets
                                             where gameListSet.ListCategoryType == ListCategoryType.Series
                                             select gameListSet;

                GameListSet seriesGameListSet = seriesGameListSetQuery?.FirstOrDefault();
                if (seriesGameListSet != null)
                {
                    foreach (string series in currentGame?.Game?.SeriesValues)
                    {
                        var seriesGameListQuery = from seriesGameList in seriesGameListSet.GameLists
                                                  where seriesGameList.ListTypeValue.Equals(series, StringComparison.InvariantCultureIgnoreCase)
                                                  select seriesGameList;

                        foreach (GameList gameList in seriesGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get lists for matching genres
                var genreGameListSetQuery = from gameListSet in mainWindowViewModel.GameListSets
                                            where gameListSet.ListCategoryType == ListCategoryType.Genre
                                            select gameListSet;

                GameListSet genreGameListSet = genreGameListSetQuery?.FirstOrDefault();
                if (genreGameListSet != null)
                {
                    foreach (string genre in currentGame?.Game?.Genres)
                    {
                        var genreGameListQuery = from genreGameList in genreGameListSet.GameLists
                                                 where genreGameList.ListTypeValue.Equals(genre, StringComparison.InvariantCultureIgnoreCase)
                                                 select genreGameList;

                        foreach (GameList gameList in genreGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get platform list 
                IEnumerable<GameListSet> platformGameListSetQuery = from gameListSet in mainWindowViewModel.GameListSets
                                                                    where gameListSet.ListCategoryType == ListCategoryType.Platform
                                                                    select gameListSet;

                GameListSet platformGameListSet = platformGameListSetQuery?.FirstOrDefault();
                if (platformGameListSet != null)
                {
                    string platform = currentGame?.Game?.Platform;
                    if (platform != null)
                    {
                        var platformGameListQuery = from platformGameList in platformGameListSet.GameLists
                                                    where platformGameList.ListTypeValue.Equals(platform, StringComparison.InvariantCultureIgnoreCase)
                                                    select platformGameList;

                        foreach (GameList gameList in platformGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Developer list 
                IEnumerable<GameListSet> developerGameListSetQuery = from gameListSet in mainWindowViewModel.GameListSets
                                                                     where gameListSet.ListCategoryType == ListCategoryType.Developer
                                                                     select gameListSet;

                GameListSet developerGameListSet = developerGameListSetQuery?.FirstOrDefault();
                if (developerGameListSet != null)
                {
                    foreach (string developer in currentGame?.Game?.Developers)
                    {
                        IEnumerable<GameList> developerGameListQuery = from developerGameList in developerGameListSet.GameLists
                                                                       where developerGameList.ListTypeValue.Equals(developer, StringComparison.InvariantCultureIgnoreCase)
                                                                       select developerGameList;

                        foreach (GameList gameList in developerGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Publisher list
                IEnumerable<GameListSet> publisherGameListSetQuery = from gameListSet in mainWindowViewModel.GameListSets
                                                                     where gameListSet.ListCategoryType == ListCategoryType.Publisher
                                                                     select gameListSet;

                GameListSet publisherGameListSet = publisherGameListSetQuery?.FirstOrDefault();
                if (publisherGameListSet != null)
                {
                    foreach (string publisher in currentGame?.Game?.Publishers)
                    {
                        IEnumerable<GameList> publisherGameListQuery = from publisherGameList in publisherGameListSet.GameLists
                                                                       where publisherGameList.ListTypeValue.Equals(publisher, StringComparison.InvariantCultureIgnoreCase)
                                                                       select publisherGameList;

                        foreach (GameList gameList in publisherGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Play mode list
                IEnumerable<GameListSet> playModeGameListSetQuery = from gameListSet in mainWindowViewModel.GameListSets
                                                                    where gameListSet.ListCategoryType == ListCategoryType.PlayMode
                                                                    select gameListSet;

                GameListSet playModeGameListSet = playModeGameListSetQuery?.FirstOrDefault();
                if (playModeGameListSet != null)
                {
                    foreach (string playMode in currentGame?.Game?.PlayModes)
                    {
                        IEnumerable<GameList> playModeGameListQuery = from playModeGameList in playModeGameListSet.GameLists
                                                                      where playModeGameList.ListTypeValue.Equals(playMode, StringComparison.InvariantCultureIgnoreCase)
                                                                      select playModeGameList;

                        foreach (GameList gameList in playModeGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // get Release year list
                IEnumerable<GameListSet> releaseYearGameListSetQuery = from gameListSet in mainWindowViewModel.GameListSets
                                                                       where gameListSet.ListCategoryType == ListCategoryType.ReleaseYear
                                                                       select gameListSet;

                GameListSet releaseYearGameListSet = releaseYearGameListSetQuery?.FirstOrDefault();
                if (releaseYearGameListSet != null)
                {
                    int? releaseYear = currentGame?.Game?.ReleaseDate?.Year;
                    if (releaseYear != null)
                    {
                        IEnumerable<GameList> releaseYearGameListQuery = from releaseYearGameList in releaseYearGameListSet.GameLists
                                                                         where releaseYearGameList.ListTypeValue.Equals(releaseYear.ToString(), StringComparison.InvariantCultureIgnoreCase)
                                                                         select releaseYearGameList;

                        foreach (GameList gameList in releaseYearGameListQuery)
                        {
                            moreLikeThisResults.Add(gameList);
                        }
                    }
                }

                // remove any prior "more like this" set and then add these results in the more like this category
                mainWindowViewModel.GameListSets.RemoveAll(set => set.ListCategoryType == ListCategoryType.MoreLikeThis);
                mainWindowViewModel.GameListSets.Add(new GameListSet
                {
                    ListCategoryType = ListCategoryType.MoreLikeThis,
                    GameLists = moreLikeThisResults
                });

                mainWindowViewModel.ResetGameLists(ListCategoryType.MoreLikeThis);
                mainWindowViewModel.UIState.IsDisplayingResults = true;
                mainWindowViewModel.UIState.IsDisplayingFeature = false;
                mainWindowViewModel.UIState.IsDisplayingMoreInfo = false;
                mainWindowViewModel.CallGameChangeFunction();
            }
        }

        public void CycleListBackward()
        {
            mainWindowViewModel.listCycle.CycleBackward();
            RefreshGameLists();
        }

        public void CycleListForward()
        {
            mainWindowViewModel.listCycle.CycleForward();
            RefreshGameLists();
        }

        public void RefreshGameLists()
        {
            if (mainWindowViewModel.listCycle?.GenericList?.Count == 0)
            {
                DisplayingErrorState displayingErrorState = mainWindowViewModel.EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
                displayingErrorState.ErrorMessage = "A problem occurred trying to refresh the list of games";
                mainWindowViewModel.EclipseStateContext.TransitionToState(displayingErrorState);
                return;
            }

            mainWindowViewModel.CurrentGameList = mainWindowViewModel.listCycle.GetItem(0);
            mainWindowViewModel.NextGameList = mainWindowViewModel.listCycle.GetItem(1);
            mainWindowViewModel.CallGameChangeFunction();
        }

        public void CheckResetGameLists()
        {
            if (mainWindowViewModel.gameListsChanged)
            {
                mainWindowViewModel.ResetListsAfterChange();
            }
        }
    }
}