using System;
using System.Collections.Generic;
using Eclipse.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// Turns the catalog into the shape the search engine works on. The boundary, and the only
    /// file below it that knows LaunchBox exists.
    ///
    /// Everything else in Service/Search takes SearchableGame and int, which is what makes the
    /// analyser, the index, the query, the scoring and the engine testable against object
    /// literals. That is not a stylistic preference here: Eclipse.Tests has no reference to the
    /// LaunchBox contract assembly at all - the Eclipse project references it with Private=false
    /// so it neither copies to output nor flows transitively - so anything taking an IGame is
    /// simply not testable in this repository.
    ///
    /// This file therefore has to stay small enough that having no tests is acceptable. Three
    /// methods, no branching worth speaking of, no rules. If logic starts collecting here, it
    /// belongs on the other side of the boundary instead.
    ///
    /// See docs/plans/text-search.md 7.2 and prerequisite P2.
    /// </summary>
    public static class SearchableGameProjection
    {
        /// <summary>
        /// The whole library, ready to index.
        ///
        /// A game's position in the catalog is its identity from here on - this is prerequisite
        /// P3, and it needs no change to GameCatalog to satisfy: the catalog already orders
        /// itself deterministically, by sort title and then by id, precisely so that downstream
        /// sorts are reproducible. That makes position stable for the life of an index, which is
        /// what lets posting lists be int[] rather than object references.
        /// </summary>
        public static IReadOnlyList<SearchableGame> FromCatalog(IGameCatalogSource catalog)
        {
            if (catalog == null)
            {
                return new SearchableGame[0];
            }

            IReadOnlyList<GameMatch> games = catalog.Games;
            List<SearchableGame> projected = new List<SearchableGame>(games.Count);

            for (int catalogIndex = 0; catalogIndex < games.Count; catalogIndex++)
            {
                SearchableGame game = From(games[catalogIndex], catalogIndex);
                if (game != null)
                {
                    projected.Add(game);
                }
            }

            return projected;
        }

        /// <summary>
        /// The whole library's metadata, ready to index for filtering.
        ///
        /// This is prerequisite P4, and like P3 it needs no change to GameCatalog. The plan
        /// anticipated adding facet posting lists there, beside the category index it already
        /// builds; doing it here instead keeps the browse interface free of search-shaped data
        /// and keeps the whole facet engine testable, because what comes out the other side is
        /// GameFacets rather than GameMatch.
        ///
        /// Reading the metadata directly costs the same as GameCatalog.Expand does - the same
        /// properties, once per game - and avoids needing a reverse map from GameMatch back to
        /// its catalog position.
        /// </summary>
        public static IReadOnlyList<GameFacets> FacetsFromCatalog(IGameCatalogSource catalog)
        {
            if (catalog == null)
            {
                return new GameFacets[0];
            }

            IReadOnlyList<GameMatch> games = catalog.Games;
            List<GameFacets> projected = new List<GameFacets>(games.Count);

            for (int catalogIndex = 0; catalogIndex < games.Count; catalogIndex++)
            {
                GameFacets facets = FacetsOf(games[catalogIndex], catalogIndex);
                if (facets != null)
                {
                    projected.Add(facets);
                }
            }

            return projected;
        }

        /// <summary>
        /// One game's metadata values, or null if there is no game there.
        ///
        /// Playlists are deliberately absent, and staying that way. Every other facet comes off
        /// the game itself, but a playlist is a relationship held elsewhere - GameCatalog gets it
        /// from PlaylistGameService - so including it would mean threading a second source
        /// through this whole path. Asked and answered in stage 4e: not wanted. Adding it later
        /// is a projection change and an entry in Facets.Filterable, nothing more.
        /// </summary>
        public static GameFacets FacetsOf(GameMatch gameMatch, int catalogIndex)
        {
            IGame game = gameMatch?.Game;
            if (game == null)
            {
                return null;
            }

            List<FacetValue> values = new List<FacetValue>();

            Add(values, ListCategoryType.Platform, game.Platform);
            Add(values, ListCategoryType.ReleaseYear, gameMatch.ReleaseYear);

            AddAll(values, ListCategoryType.Series, game.SeriesValues);
            AddAll(values, ListCategoryType.Genre, game.Genres);
            AddAll(values, ListCategoryType.Publisher, game.Publishers);
            AddAll(values, ListCategoryType.Developer, game.Developers);
            AddAll(values, ListCategoryType.PlayMode, game.PlayModes);

            return new GameFacets(catalogIndex, values);
        }

        private static void Add(List<FacetValue> values, ListCategoryType facet, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(new FacetValue(facet, value));
            }
        }

        private static void AddAll(List<FacetValue> values, ListCategoryType facet, IEnumerable<string> from)
        {
            if (from == null)
            {
                return;
            }

            foreach (string value in from)
            {
                Add(values, facet, value);
            }
        }

        /// <summary>
        /// Turns search results back into the catalog's own games, in rank order.
        ///
        /// The return leg of the boundary. The engine deals in catalog indices and knows nothing
        /// of GameMatch; everything above the boundary - the results row, the committed list -
        /// needs the real games. Both directions of that mapping live in this one file so there
        /// is a single place where an index is assumed to mean a position.
        ///
        /// Hits pointing outside the catalog are skipped rather than throwing. That cannot
        /// happen while the index and the catalog are the same age, which they are for the whole
        /// session - but the cost of being wrong here is a crash on a keystroke.
        /// </summary>
        public static List<GameMatch> ToGameMatches(IGameCatalogSource catalog, IEnumerable<SearchHit> hits)
        {
            List<GameMatch> games = new List<GameMatch>();

            if (catalog == null || hits == null)
            {
                return games;
            }

            IReadOnlyList<GameMatch> all = catalog.Games;

            foreach (SearchHit hit in hits)
            {
                if (hit.CatalogIndex >= 0 && hit.CatalogIndex < all.Count)
                {
                    games.Add(all[hit.CatalogIndex]);
                }
            }

            return games;
        }

        /// <summary>
        /// One game at a known catalog position, or null if there is no game there.
        /// </summary>
        public static SearchableGame From(GameMatch gameMatch, int catalogIndex)
        {
            IGame game = gameMatch?.Game;
            if (game == null)
            {
                return null;
            }

            return new SearchableGame(catalogIndex, game.Title, PopularityOf(game));
        }

        /// <summary>
        /// How well regarded and well played a game is, as 0..SearchableGame.MaxPopularity.
        ///
        /// Rating carries most of it and being played at all carries the last point, so a game
        /// the user actually returns to edges ahead of an equally rated one they have never
        /// started. Like every other number in the ranking this is an initial value - see
        /// SearchScoring - and it is only ever a tiebreak: the whole range is smaller than the
        /// gap between one match quality and the next.
        /// </summary>
        public static int PopularityOf(IGame game)
        {
            if (game == null)
            {
                return 0;
            }

            int stars = (int)Math.Round((double)game.CommunityOrLocalStarRating, MidpointRounding.AwayFromZero);

            if (stars < 0)
            {
                stars = 0;
            }

            if (stars > SearchableGame.MaxPopularity - 1)
            {
                stars = SearchableGame.MaxPopularity - 1;
            }

            return stars + (game.PlayCount > 0 ? 1 : 0);
        }
    }
}
