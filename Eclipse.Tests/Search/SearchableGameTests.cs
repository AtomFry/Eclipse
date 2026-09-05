using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// The projection the whole engine works on. Small, but its two invariants are relied on
    /// everywhere: tokens always agree with the title, and popularity is always in range.
    /// </summary>
    public class SearchableGameTests
    {
        [Fact]
        public void Tokens_are_derived_from_the_title_rather_than_supplied()
        {
            SearchableGame game = new SearchableGame(0, "Sonic & Knuckles", 0);

            Assert.Equal("Sonic & Knuckles", game.DisplayTitle);
            Assert.Equal(new[] { "sonic", "and", "knuckles" }, game.TitleTokens);
        }

        /// <summary>
        /// In title order, because two ranking components depend on position and length: the
        /// first-token bonus asks whether the query matched the start of the title, and the
        /// brevity bonus counts how many tokens there are.
        /// </summary>
        [Fact]
        public void Tokens_are_kept_in_title_order()
        {
            SearchableGame game = new SearchableGame(0, "The Legend of Zelda", 0);

            Assert.Equal(new[] { "the", "legend", "of", "zelda" }, game.TitleTokens);
        }

        [Theory]
        [InlineData(-5, 0)]
        [InlineData(0, 0)]
        [InlineData(3, 3)]
        [InlineData(5, 5)]
        [InlineData(99, 5)]
        public void Popularity_is_clamped_into_range(int supplied, int expected)
        {
            Assert.Equal(expected, new SearchableGame(0, "Sonic", supplied).Popularity);
        }

        [Fact]
        public void A_null_title_is_an_empty_title_with_no_tokens()
        {
            SearchableGame game = new SearchableGame(0, null, 0);

            Assert.Equal(string.Empty, game.DisplayTitle);
            Assert.Empty(game.TitleTokens);
        }

        [Fact]
        public void A_title_of_only_punctuation_has_no_tokens()
        {
            Assert.Empty(new SearchableGame(0, "!!! ???", 0).TitleTokens);
        }

        /// <summary>
        /// The catalog index is carried through untouched - it is an identity, not a position in
        /// any list this type knows about.
        /// </summary>
        [Fact]
        public void The_catalog_index_is_carried_through_unchanged()
        {
            Assert.Equal(4271, new SearchableGame(4271, "Sonic", 0).CatalogIndex);
        }
    }
}
