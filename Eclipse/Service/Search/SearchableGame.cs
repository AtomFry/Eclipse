using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// One game as the search engine sees it, and the only shape the engine ever works on.
    ///
    /// This is the seam that keeps search testable. Nothing below the adapter that builds these
    /// knows that LaunchBox exists, so the index, the query evaluation and the ranking can all
    /// be exercised against object literals with no Big Box behind them - which is not a
    /// theoretical benefit here: Eclipse.Tests has no reference to the LaunchBox contract
    /// assembly at all, so anything taking an IGame is not testable in this repository.
    ///
    /// See docs/plans/text-search.md 7.2 (the seam) and P2 (why it comes before the engine).
    /// </summary>
    public sealed class SearchableGame
    {
        /// <summary>The most a game's popularity can contribute to its score.</summary>
        public const int MaxPopularity = 5;

        /// <param name="catalogIndex">
        /// The game's position in GameCatalog.Games, which is the identity everything downstream
        /// uses. The catalog orders itself deterministically - by sort title, then by id - so
        /// that downstream sorts are reproducible; that makes position a stable identity for
        /// the lifetime of an index, and lets posting lists be int[] rather than references.
        /// </param>
        /// <param name="displayTitle">The title as the user sees it, for display only.</param>
        /// <param name="popularity">Clamped to 0..MaxPopularity. A ranking tiebreak.</param>
        public SearchableGame(int catalogIndex, string displayTitle, int popularity)
        {
            CatalogIndex = catalogIndex;
            DisplayTitle = displayTitle ?? string.Empty;
            TitleTokens = TextAnalyzer.Tokenize(DisplayTitle);
            Popularity = Clamp(popularity);
        }

        public int CatalogIndex { get; }

        /// <summary>The title as it is shown. Never analysed text - use TitleTokens for that.</summary>
        public string DisplayTitle { get; }

        /// <summary>
        /// The analysed tokens of the display title, in title order. Always derived from
        /// DisplayTitle rather than supplied, so the two cannot disagree.
        ///
        /// In title order because two ranking components depend on position and length: the
        /// first-token bonus asks whether the query matched the start of the title, and the
        /// brevity bonus counts how many tokens there are.
        ///
        /// These are the title's own tokens, not its index terms. A game is findable by more
        /// terms than appear here - see TextAnalyzer.IndexTerms - and scoring accounts for that
        /// rather than assuming a matched term appears literally in the title.
        /// </summary>
        public IReadOnlyList<string> TitleTokens { get; }

        /// <summary>How well regarded and well played the game is, 0..MaxPopularity.</summary>
        public int Popularity { get; }

        public override string ToString()
        {
            return $"[{CatalogIndex}] {DisplayTitle}";
        }

        private static int Clamp(int popularity)
        {
            if (popularity < 0)
            {
                return 0;
            }

            if (popularity > MaxPopularity)
            {
                return MaxPopularity;
            }

            return popularity;
        }
    }
}
