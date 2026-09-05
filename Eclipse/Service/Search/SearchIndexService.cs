using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Eclipse.Helpers;
using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// Builds the search index once, in the background, and hands it out when it is ready.
    ///
    /// Deliberately the same shape as SpeechRecognizerService, because it has the same job: work
    /// that only matters if the user actually searches, and which must not hold up the first
    /// screen. Both are started from LoadingState, both report an availability the screen can
    /// say something about, and both publish their result before flipping the flag that says it
    /// exists.
    ///
    /// LIFECYCLE. Built once and held for the life of the process. There is nothing to dispose -
    /// the index is managed arrays and strings - and nothing to cancel, because the build is a
    /// single pass that finishes in well under the time it takes a user to reach the search
    /// screen. Big Box has no plugin shutdown callback to dispose against in any case; this
    /// matches how GameCatalog, VoiceSearchIndex and SpeechRecognizerService already live.
    ///
    /// NOT REBUILT. Catalog indices are the index's identity and GameCatalog builds its game
    /// list exactly once (it latches on isSetup), so positions are stable for the whole session.
    /// Favouriting or rating a game rebuilds the browsing <em>lists</em>, not the catalog, and
    /// changes no title - so the index stays correct and is never rebuilt, least of all per
    /// keystroke.
    /// </summary>
    public sealed class SearchIndexService : ISearchIndexSource
    {
        private volatile SearchAvailability availability = SearchAvailability.Preparing;

        // Written before availability is set to Ready. availability is volatile, so a reader
        // that sees Ready is guaranteed to see a fully built index.
        private TitleIndex index;

        private long buildMilliseconds;
        private int indexedGameCount;

        /// <summary>Whether a search can run right now.</summary>
        public SearchAvailability Availability => availability;

        /// <summary>The index, or null unless Availability is Ready.</summary>
        public ISearchIndex Index => availability == SearchAvailability.Ready ? index : null;

        /// <summary>How long the build took. Zero until it has finished.</summary>
        public long BuildMilliseconds => buildMilliseconds;

        /// <summary>How many games went into the index. Zero until it has finished.</summary>
        public int IndexedGameCount => indexedGameCount;

        /// <summary>
        /// Starts the build away from the startup path. Returns immediately; text search reports
        /// itself as preparing until the work finishes.
        /// </summary>
        public void PrepareInBackground()
        {
            PrepareInBackground(GameCatalog.Instance,
                                EclipseSettingsDataProvider.Instance.EclipseSettings.EnableTextSearch);
        }

        /// <summary>
        /// The testable form. Takes the catalog and the setting rather than reaching for the
        /// singletons, so the availability transitions can be exercised without a Big Box.
        /// </summary>
        public void PrepareInBackground(IGameCatalogSource catalog, bool enabled)
        {
            if (!enabled)
            {
                availability = SearchAvailability.Disabled;
                return;
            }

            availability = SearchAvailability.Preparing;
            Task.Run(() => Prepare(catalog));
        }

        /// <summary>
        /// Builds synchronously. Exposed for tests, which want the finished state rather than a
        /// race; production always goes through PrepareInBackground.
        /// </summary>
        public void Prepare(IGameCatalogSource catalog)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                // Touching the catalog is what builds it if nothing else has yet. That is safe
                // from here - GameCatalog guards its setup with a lock, so arriving during the
                // startup thread's own build waits rather than building twice.
                IReadOnlyList<SearchableGame> games = SearchableGameProjection.FromCatalog(catalog);
                TitleIndex built = TitleIndex.Build(games);

                stopwatch.Stop();

                // Assign before publishing the status, so a reader that sees Ready sees the
                // whole index.
                index = built;
                buildMilliseconds = stopwatch.ElapsedMilliseconds;
                indexedGameCount = games.Count;
                availability = SearchAvailability.Ready;

                LogHelper.Log($"Text search index built: {games.Count} games, {built.TermCount} terms, {buildMilliseconds}ms");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "build text search index");

                // One attempt. The build has no external dependency that could come right on a
                // retry - it is the catalog and some string work - so a failure here means
                // something is wrong that trying again will not fix. The user is told text
                // search is unavailable; the detail is in the log.
                availability = SearchAvailability.Failed;
            }
        }

        #region singleton implementation
        public static SearchIndexService Instance => instance;

        private static readonly SearchIndexService instance = new SearchIndexService();

        static SearchIndexService()
        {
        }

        private SearchIndexService()
        {
        }
        #endregion
    }
}
