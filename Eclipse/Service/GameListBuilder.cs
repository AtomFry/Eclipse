using System;
using Eclipse.Models;
using System.Collections.Generic;
using System.Linq;

namespace Eclipse.Service
{
    // Builds the sets of game lists the user browses: games by platform, by genre, by developer
    // and so on, each set carrying the user's own custom lists first and then one list per
    // distinct value of that category.
    //
    // Extracted from MainWindowViewModel (B-15a). It takes what it needs rather than reaching
    // for it - a catalog, the custom list definitions, and which playlists have asked to appear
    // alongside platforms - so that what it produces can be checked without a running BigBox.
    public sealed class GameListBuilder
    {
        // The eight categories a set is built for, in the order the sets are built. Not every
        // ListCategoryType appears: VoiceSearch, RandomGame and MoreLikeThis are assembled
        // elsewhere from results rather than from the catalog.
        public static readonly IReadOnlyList<ListCategoryType> BrowsableCategories = new[]
        {
            ListCategoryType.Platform,
            ListCategoryType.ReleaseYear,
            ListCategoryType.Genre,
            ListCategoryType.Publisher,
            ListCategoryType.Developer,
            ListCategoryType.Series,
            ListCategoryType.PlayMode,
            ListCategoryType.Playlist
        };

        private static readonly IReadOnlyDictionary<string, bool> NoPlaylists = new Dictionary<string, bool>();

        private readonly IGameCatalogSource catalog;
        private readonly IReadOnlyList<CustomListDefinition> customListDefinitions;
        private readonly IReadOnlyDictionary<string, bool> playlistInclusion;

        /// <param name="playlistInclusion">
        /// Playlist name to whether that playlist asked to be shown with platforms. Only the
        /// platform set consults it.
        /// </param>
        public GameListBuilder(IGameCatalogSource catalog,
                               IReadOnlyList<CustomListDefinition> customListDefinitions,
                               IReadOnlyDictionary<string, bool> playlistInclusion)
        {
            this.catalog = catalog;
            this.customListDefinitions = customListDefinitions ?? new List<CustomListDefinition>();
            this.playlistInclusion = playlistInclusion ?? NoPlaylists;
        }

        public List<GameListSet> BuildAll()
        {
            List<GameListSet> gameListSets = new List<GameListSet>(BrowsableCategories.Count);

            foreach (ListCategoryType listCategoryType in BrowsableCategories)
            {
                gameListSets.Add(Build(listCategoryType));
            }

            return gameListSets;
        }

        public GameListSet Build(ListCategoryType listCategoryType)
        {
            List<GameList> listOfGameList = new List<GameList>();

            IEnumerable<CustomListDefinition> filteredCustomListDefinitions =
                from customListDefinition in customListDefinitions
                where customListDefinition.ListCategoryTypes.Contains(listCategoryType)
                select customListDefinition;

            int sortOrder = 0;
            foreach (CustomListDefinition customListDefinition in filteredCustomListDefinitions)
            {
                // custom lists filter the whole library - every game once, which is what the
                // platform-category projection amounted to since each game has one platform
                IQueryable<GameMatch> baseQuery = catalog.Games.AsQueryable();

                if (customListDefinition.FilterExpressions.Any())
                {
                    foreach (FilterExpression filterExpression in customListDefinition.FilterExpressions)
                    {
                        baseQuery = baseQuery.ApplyFilter(filterExpression);
                    }
                }

                // The default order, replaced outright by the first sort expression if the list
                // defines any - later expressions refine that one rather than this.
                IOrderedQueryable<GameMatch> orderedQuery = baseQuery.OrderBy(gameMatch => gameMatch.Game.SortTitleOrTitle);

                if (customListDefinition.SortExpressions.Any())
                {
                    bool first = true;
                    foreach (SortExpression sortExpression in customListDefinition.SortExpressions)
                    {
                        IGameFieldAccessor accessor = GameFields.For(sortExpression.GameFieldEnum);

                        // Neither switch has a default: a direction outside the enum leaves the
                        // order as it was, which is what a value hand-edited into
                        // CustomLists.json has always done.
                        if (first)
                        {
                            first = false;
                            switch (sortExpression.SortDirection)
                            {
                                case SortDirection.Ascending:
                                    orderedQuery = accessor.OrderBy(baseQuery);
                                    break;
                                case SortDirection.Descending:
                                    orderedQuery = accessor.OrderByDescending(baseQuery);
                                    break;
                            }
                        }
                        else
                        {
                            switch (sortExpression.SortDirection)
                            {
                                case SortDirection.Ascending:
                                    orderedQuery = accessor.ThenBy(orderedQuery);
                                    break;
                                case SortDirection.Descending:
                                    orderedQuery = accessor.ThenByDescending(orderedQuery);
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

                // Materialise once. This used to run the query twice - once to ask whether it
                // matched anything and again to read the matches - so every filter delegate ran
                // over the whole library twice, on every rebuild, and a rebuild happens on every
                // favourite, rating change and game launch.
                List<GameMatch> matchingGames = baseQuery.ToList();

                if (matchingGames.Count > 0)
                {
                    listOfGameList.Add(new GameList(customListDefinition.Description, matchingGames, sortOrder++));
                }
            }

            foreach (IGrouping<string, GameMatch> gameGroup in catalog.ByCategory(listCategoryType))
            {
                listOfGameList.Add(new GameList(gameGroup.Key, gameGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList()));
            }

            // include playlists in platforms if they are set to be included
            if (listCategoryType == ListCategoryType.Platform)
            {
                foreach (IGrouping<string, GameMatch> gameGroup in catalog.ByCategory(ListCategoryType.Playlist))
                {
                    bool includeInPlaylists = false;
                    if (playlistInclusion.TryGetValue(gameGroup.Key, out includeInPlaylists))
                    {
                        if (includeInPlaylists)
                        {
                            listOfGameList.Add(new GameList(gameGroup.Key, gameGroup.OrderBy(game => game.Game.SortTitleOrTitle).ToList()));
                        }
                    }
                }
            }

            return new GameListSet
            {
                GameLists = listOfGameList.OrderBy(list => list.SortOrder)
                                            .ThenBy(list => list.ListTypeValue).ToList(),
                ListCategoryType = listCategoryType
            };
        }

        // The categories "more like this" looks in, and how to get the current game's values for
        // each. The order of this table is the order the results appear in - which is the
        // question OQ-003 asks about, now visible in one place instead of implied by the order
        // of seven copy-pasted blocks.
        //
        // Platform and release year yield at most one value; the rest yield the game's whole
        // collection. Neither collection is null-guarded, because it never was: a metadata
        // property returning null has always thrown here rather than silently matching nothing.
        private static readonly (ListCategoryType Category, Func<GameMatch, IEnumerable<string>> ValuesOf)[] MoreLikeThisCategories =
        {
            (ListCategoryType.Series, gameMatch => gameMatch?.Game?.SeriesValues),
            (ListCategoryType.Genre, gameMatch => gameMatch?.Game?.Genres),
            (ListCategoryType.Platform, gameMatch => Only(gameMatch?.Game?.Platform)),
            (ListCategoryType.Developer, gameMatch => gameMatch?.Game?.Developers),
            (ListCategoryType.Publisher, gameMatch => gameMatch?.Game?.Publishers),
            (ListCategoryType.PlayMode, gameMatch => gameMatch?.Game?.PlayModes),
            (ListCategoryType.ReleaseYear, gameMatch => Only(gameMatch?.Game?.ReleaseDate?.Year.ToString()))
        };

        // Picks the lists to show for "more like this": every list, in every category set, whose
        // value the current game shares.
        //
        // The lists are the ones that already exist in the other sets rather than new ones, so
        // the same GameList instance ends up referenced from two sets. Duplicates are not
        // removed - a game sharing two genres with a list's category value contributes that list
        // twice - and the caller has always shown them as they come.
        public static List<GameList> BuildMoreLikeThis(GameMatch currentGame, IReadOnlyList<GameListSet> gameListSets)
        {
            List<GameList> moreLikeThisResults = new List<GameList>();

            foreach (var categoryValues in MoreLikeThisCategories)
            {
                GameListSet categorySet = gameListSets.FirstOrDefault(set => set.ListCategoryType == categoryValues.Category);
                if (categorySet == null)
                {
                    continue;
                }

                foreach (string value in categoryValues.ValuesOf(currentGame))
                {
                    moreLikeThisResults.AddRange(categorySet.GameLists
                        .Where(gameList => gameList.ListTypeValue.Equals(value, StringComparison.InvariantCultureIgnoreCase)));
                }
            }

            return moreLikeThisResults;
        }

        // The one-value categories, as a sequence of none or one, so the table can treat every
        // category the same way. This is what the old platform and release year blocks did with
        // their null checks.
        private static IEnumerable<string> Only(string value)
        {
            if (value != null)
            {
                yield return value;
            }
        }
    }
}
