using System.Collections.Generic;
using System.Linq;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-013 - query evaluation: the AND across terms, the ordering, and the contract
    /// that the result is always a ranked, bounded, deterministic list of catalog indices.
    /// </summary>
    public class SearchEngineTests
    {
        private static TitleIndex IndexOf(params string[] titles)
        {
            List<SearchableGame> games = new List<SearchableGame>();
            for (int index = 0; index < titles.Length; index++)
            {
                games.Add(new SearchableGame(index, titles[index], 0));
            }

            return TitleIndex.Build(games);
        }

        private static int[] Search(TitleIndex index, string buffer, int maxResults = SearchEngine.DefaultMaxResults)
        {
            return SearchEngine.Search(SearchQuery.Parse(buffer), index, maxResults)
                               .Select(hit => hit.CatalogIndex)
                               .ToArray();
        }

        /// <summary>
        /// The games a search found, in catalog order rather than rank order - for the tests
        /// about <em>which</em> games matched, which must not fail when the ranking is tuned.
        /// Ordering has its own tests below.
        /// </summary>
        private static int[] Found(TitleIndex index, string buffer)
        {
            int[] found = Search(index, buffer);
            System.Array.Sort(found);
            return found;
        }

        // ------------------------------------------------------------------
        // Nothing to search, or nothing found.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void An_empty_query_finds_nothing_rather_than_everything(string buffer)
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Super Mario Bros");

            Assert.Empty(Search(index, buffer));
        }

        [Fact]
        public void A_null_index_or_query_finds_nothing()
        {
            Assert.Empty(SearchEngine.Search(SearchQuery.Parse("sonic"), null));
            Assert.Empty(SearchEngine.Search(null, IndexOf("Sonic")));
        }

        [Fact]
        public void A_query_matching_no_game_finds_nothing()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Super Mario Bros");

            Assert.Empty(Search(index, "zelda"));
        }

        [Fact]
        public void Searching_an_empty_library_finds_nothing()
        {
            Assert.Empty(Search(TitleIndex.Build(new SearchableGame[0]), "sonic"));
        }

        // ------------------------------------------------------------------
        // The AND.
        // ------------------------------------------------------------------

        /// <summary>
        /// VER-SEARCH-013. Two terms narrow rather than widen - the defining behaviour of a
        /// search box, and the opposite of what the voice path does with several phrases.
        /// </summary>
        [Fact]
        public void Two_terms_narrow_rather_than_widen()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure", "Mario Kart");

            Assert.Equal(new[] { 0, 1 }, Found(index, "sonic "));
            Assert.Equal(new[] { 0 }, Found(index, "sonic hedgehog "));
        }

        [Fact]
        public void A_term_matching_nothing_empties_the_whole_result()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure");

            Assert.Empty(Search(index, "sonic zelda"));
        }

        [Fact]
        public void Completed_terms_and_a_prefix_term_combine()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure", "Sonic Spinball");

            Assert.Equal(new[] { 0 }, Search(index, "sonic hedge"));
            Assert.Equal(new[] { 1 }, Search(index, "sonic adv"));
        }

        [Fact]
        public void Word_order_in_the_query_does_not_change_the_result()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure");

            Assert.Equal(Search(index, "sonic hedgehog "), Search(index, "hedgehog sonic "));
        }

        /// <summary>
        /// A completed term is matched exactly, so it does not silently widen to longer terms
        /// the way the term still being typed does.
        /// </summary>
        [Fact]
        public void A_completed_term_does_not_match_a_longer_word()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog");

            Assert.Empty(Search(index, "son "));
            Assert.NotEmpty(Search(index, "son"));
        }

        // ------------------------------------------------------------------
        // Ranking.
        // ------------------------------------------------------------------

        /// <summary>
        /// All three match "sonic" equally well and none has a rating, so the order is decided
        /// by brevity first and position only after it.
        ///
        /// Note what that means for the last two: "Adventures of Sonic" beats the long
        /// collection despite not starting with the word, because a three word title beats a
        /// seven word one and position is the weakest signal there is. That is the deliberate
        /// consequence of the ranking rework - the same ordering that stops "Zelda II" beating
        /// "The Legend of Zelda".
        /// </summary>
        [Fact]
        public void Results_come_back_best_first()
        {
            TitleIndex index = IndexOf(
                "Sonic the Hedgehog 2 Special Edition Collection",   // 0 - seven words
                "Sonic the Hedgehog",                                // 1 - three, starts with it
                "Adventures of Sonic");                              // 2 - three, matches late

            int[] found = Search(index, "sonic");

            Assert.Equal(1, found[0]);
            Assert.Equal(2, found[1]);
            Assert.Equal(0, found[2]);
        }

        [Fact]
        public void Ranks_descend_through_the_result_list()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure Collection Special Edition", "Sonata");

            IReadOnlyList<SearchHit> hits = SearchEngine.Search(SearchQuery.Parse("son"), index);

            for (int index2 = 1; index2 < hits.Count; index2++)
            {
                Assert.True(hits[index2 - 1].Rank.CompareTo(hits[index2].Rank) >= 0);
            }
        }

        [Fact]
        public void Every_result_carries_the_rank_that_put_it_there()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure");

            IReadOnlyList<SearchHit> hits = SearchEngine.Search(SearchQuery.Parse("son"), index);

            Assert.All(hits, hit => Assert.True(hit.Rank.IsMatch));
        }

        /// <summary>
        /// Ties break on catalog index, which is the library's own sort-title order - so an
        /// identical pair of games always comes out in the same sequence rather than in whatever
        /// order the intersection happened to produce.
        /// </summary>
        [Fact]
        public void Games_that_score_the_same_come_back_in_catalog_order()
        {
            TitleIndex index = IndexOf("Sonic Alpha", "Sonic Bravo", "Sonic Delta");

            Assert.Equal(new[] { 0, 1, 2 }, Search(index, "sonic"));
        }

        [Fact]
        public void The_same_search_twice_gives_the_same_answer()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure", "Sonic Spinball", "Sonata");

            Assert.Equal(Search(index, "son"), Search(index, "son"));
        }

        // ------------------------------------------------------------------
        // Bounding.
        // ------------------------------------------------------------------

        [Fact]
        public void The_result_is_capped_at_the_requested_size()
        {
            TitleIndex index = IndexOf("Sonic A", "Sonic B", "Sonic C", "Sonic D", "Sonic E");

            Assert.Equal(2, Search(index, "sonic", 2).Length);
        }

        [Fact]
        public void A_cap_keeps_the_best_results_not_the_first_ones_found()
        {
            TitleIndex index = IndexOf(
                "Sonic Adventure Collection Special Edition Deluxe",  // 0 - long, scores lowest
                "Sonic");                                             // 1 - short, scores highest

            Assert.Equal(new[] { 1 }, Search(index, "sonic", 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void A_cap_of_zero_or_less_returns_everything(int maxResults)
        {
            TitleIndex index = IndexOf("Sonic A", "Sonic B", "Sonic C");

            Assert.Equal(3, Search(index, "sonic", maxResults).Length);
        }

        // ------------------------------------------------------------------
        // Numeral folding, end to end through the index and the scorer.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("final fantasy vii ")]
        [InlineData("final fantasy 7 ")]
        [InlineData("final fantasy seven ")]
        public void A_numbered_title_is_findable_by_any_spelling_of_its_number(string buffer)
        {
            TitleIndex index = IndexOf("Final Fantasy VII", "Final Fantasy VIII");

            Assert.Equal(new[] { 0 }, Found(index, buffer));
        }

        [Fact]
        public void A_numeral_query_does_not_drag_in_the_neighbouring_number()
        {
            TitleIndex index = IndexOf("Final Fantasy VII", "Final Fantasy VIII");

            Assert.Equal(new[] { 1 }, Found(index, "final fantasy 8 "));
        }

        /// <summary>
        /// While a term is still being typed it is a prefix, and roman numerals are prefixes of
        /// each other - so "vii" mid-typing legitimately reaches VIII as well, exactly as "son"
        /// reaches "sonata". Typing the space that completes the term narrows it back down.
        ///
        /// Worth pinning because it looks like a bug and is not: the alternative is a term that
        /// only matches on the keystroke that happens to finish a word.
        /// </summary>
        [Fact]
        public void A_roman_numeral_still_being_typed_reaches_the_longer_numerals_too()
        {
            TitleIndex index = IndexOf("Final Fantasy VII", "Final Fantasy VIII");

            Assert.Equal(new[] { 0, 1 }, Found(index, "final fantasy vii"));
            Assert.Equal(new[] { 0 }, Found(index, "final fantasy vii "));
        }

        /// <summary>
        /// The digit spellings do not share prefixes the way the roman ones do, so typing "7"
        /// narrows immediately where "vii" does not.
        /// </summary>
        [Fact]
        public void The_digit_spelling_narrows_without_waiting_for_the_space()
        {
            TitleIndex index = IndexOf("Final Fantasy VII", "Final Fantasy VIII");

            Assert.Equal(new[] { 0 }, Found(index, "final fantasy 7"));
        }

        // ------------------------------------------------------------------
        // The index and the scorer must not disagree.
        // ------------------------------------------------------------------

        /// <summary>
        /// If a hand-rolled index claims a game carries a term its title cannot be scored
        /// against, the engine drops it rather than ranking it last. A result the user cannot
        /// see the reason for is worse than one that is missing.
        /// </summary>
        [Fact]
        public void A_game_the_index_claims_but_scoring_rejects_is_dropped()
        {
            Assert.Empty(SearchEngine.Search(SearchQuery.Parse("zelda"), new LyingIndex()));
        }

        private sealed class LyingIndex : ISearchIndex
        {
            private readonly SearchableGame game = new SearchableGame(0, "Sonic the Hedgehog", 0);

            public IReadOnlyList<int> Exact(string term) => new[] { 0 };
            public IReadOnlyList<int> Prefix(string prefix) => new[] { 0 };
            public SearchableGame Game(int catalogIndex) => catalogIndex == 0 ? game : null;
        }

        /// <summary>
        /// A posting pointing at a game the index cannot produce is skipped rather than throwing.
        /// </summary>
        [Fact]
        public void A_posting_with_no_game_behind_it_is_skipped()
        {
            Assert.Empty(SearchEngine.Search(SearchQuery.Parse("sonic"), new EmptyHandedIndex()));
        }

        private sealed class EmptyHandedIndex : ISearchIndex
        {
            public IReadOnlyList<int> Exact(string term) => new[] { 7 };
            public IReadOnlyList<int> Prefix(string prefix) => new[] { 7 };
            public SearchableGame Game(int catalogIndex) => null;
        }
    }
}
