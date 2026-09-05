using System.Collections.Generic;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// The inverted index: exact lookup, prefix range, and the posting-list invariants that
    /// everything downstream assumes (ascending, distinct).
    /// </summary>
    public class TitleIndexTests
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

        [Fact]
        public void An_exact_term_finds_the_games_carrying_it()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Super Mario Bros", "Sonic Adventure");

            Assert.Equal(new[] { 0, 2 }, index.Exact("sonic"));
            Assert.Equal(new[] { 1 }, index.Exact("mario"));
        }

        [Fact]
        public void A_term_no_game_carries_finds_nothing()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog");

            Assert.Empty(index.Exact("zelda"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void An_empty_term_finds_nothing(string term)
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog");

            Assert.Empty(index.Exact(term));
            Assert.Empty(index.Prefix(term));
        }

        /// <summary>
        /// Exact means exact. A term that is only a prefix of an indexed term is not an exact
        /// match - that is what Prefix is for, and conflating them would make every completed
        /// query term silently widen.
        /// </summary>
        [Fact]
        public void An_exact_lookup_does_not_match_a_longer_term()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog");

            Assert.Empty(index.Exact("son"));
            Assert.NotEmpty(index.Exact("sonic"));
        }

        [Fact]
        public void A_prefix_finds_every_game_whose_term_starts_with_it()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Sonic Adventure", "Super Mario Bros", "Sonata");

            Assert.Equal(new[] { 0, 1, 3 }, index.Prefix("son"));
            Assert.Equal(new[] { 0, 1 }, index.Prefix("sonic"));
        }

        [Fact]
        public void A_prefix_matching_no_term_finds_nothing()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog");

            Assert.Empty(index.Prefix("zel"));
        }

        /// <summary>
        /// A term is a prefix of itself, which is what keeps the last typed term working on the
        /// keystroke that completes a word as well as the ones before it.
        /// </summary>
        [Fact]
        public void A_whole_term_is_a_prefix_of_itself()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog");

            Assert.Equal(new[] { 0 }, index.Prefix("sonic"));
        }

        [Fact]
        public void A_single_letter_prefix_reaches_every_term_starting_with_it()
        {
            TitleIndex index = IndexOf("Sonic", "Streets of Rage", "Mario", "Super Metroid");

            Assert.Equal(new[] { 0, 1, 3 }, index.Prefix("s"));
        }

        /// <summary>
        /// Postings must be ascending and distinct: intersection relies on the order, and a game
        /// whose title repeats a word must not appear twice.
        /// </summary>
        [Fact]
        public void A_game_carrying_a_term_twice_appears_in_its_postings_once()
        {
            TitleIndex index = IndexOf("Sonic Sonic Sonic");

            Assert.Equal(new[] { 0 }, index.Exact("sonic"));
        }

        [Fact]
        public void Postings_are_ascending_across_a_prefix_union()
        {
            TitleIndex index = IndexOf("Sonata", "Sonic Adventure", "Song", "Sonic the Hedgehog");

            Assert.Equal(new[] { 0, 1, 2, 3 }, index.Prefix("son"));
        }

        /// <summary>
        /// A game is findable by the other spellings of any number in its title, which is the
        /// point of index-time numeral folding.
        /// </summary>
        [Fact]
        public void A_game_is_findable_by_every_spelling_of_a_number_in_its_title()
        {
            TitleIndex index = IndexOf("Final Fantasy VII");

            Assert.Equal(new[] { 0 }, index.Exact("vii"));
            Assert.Equal(new[] { 0 }, index.Exact("7"));
            Assert.Equal(new[] { 0 }, index.Exact("seven"));
        }

        [Fact]
        public void Numeral_folding_works_from_the_digit_form_too()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog 2");

            Assert.Equal(new[] { 0 }, index.Exact("2"));
            Assert.Equal(new[] { 0 }, index.Exact("ii"));
            Assert.Equal(new[] { 0 }, index.Exact("two"));
        }

        [Fact]
        public void A_game_can_be_retrieved_by_its_catalog_index()
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Super Mario Bros");

            Assert.Equal("Sonic the Hedgehog", index.Game(0).DisplayTitle);
            Assert.Equal("Super Mario Bros", index.Game(1).DisplayTitle);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        [InlineData(9999)]
        public void An_index_outside_the_library_has_no_game(int catalogIndex)
        {
            TitleIndex index = IndexOf("Sonic the Hedgehog", "Super Mario Bros");

            Assert.Null(index.Game(catalogIndex));
        }

        /// <summary>
        /// Catalog indices are an identity, not a position in the list handed to Build - so a
        /// sparse or out-of-order set of games indexes correctly.
        /// </summary>
        [Fact]
        public void Catalog_indices_need_not_be_dense_or_ordered()
        {
            TitleIndex index = TitleIndex.Build(new[]
            {
                new SearchableGame(40, "Sonic Adventure", 0),
                new SearchableGame(10, "Sonic the Hedgehog", 0)
            });

            Assert.Equal(new[] { 10, 40 }, index.Prefix("sonic"));
            Assert.Equal("Sonic the Hedgehog", index.Game(10).DisplayTitle);
            Assert.Null(index.Game(20));
        }

        [Fact]
        public void An_empty_library_builds_an_empty_index()
        {
            TitleIndex index = TitleIndex.Build(new SearchableGame[0]);

            Assert.Equal(0, index.TermCount);
            Assert.Empty(index.Exact("sonic"));
            Assert.Empty(index.Prefix("son"));
            Assert.Null(index.Game(0));
        }

        [Fact]
        public void A_null_library_builds_an_empty_index()
        {
            TitleIndex index = TitleIndex.Build(null);

            Assert.Equal(0, index.TermCount);
            Assert.Empty(index.Prefix("son"));
        }

        [Fact]
        public void A_null_game_in_the_library_is_skipped()
        {
            TitleIndex index = TitleIndex.Build(new[]
            {
                new SearchableGame(0, "Sonic the Hedgehog", 0),
                null,
                new SearchableGame(2, "Sonic Adventure", 0)
            });

            Assert.Equal(new[] { 0, 2 }, index.Prefix("sonic"));
        }

        [Fact]
        public void A_game_with_an_empty_title_contributes_no_terms()
        {
            TitleIndex index = IndexOf("Sonic", "", "   ");

            Assert.Equal(1, index.TermCount);
            Assert.Equal(new[] { 0 }, index.Exact("sonic"));
        }
    }
}
