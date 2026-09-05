using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// How a typed buffer becomes terms, and specifically which term is treated as a prefix.
    /// Part of VER-SEARCH-013's AND behaviour and the foundation the whole as-you-type
    /// experience rests on.
    /// </summary>
    public class SearchQueryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("!!!")]
        public void A_buffer_with_no_words_is_an_empty_query(string buffer)
        {
            SearchQuery query = SearchQuery.Parse(buffer);

            Assert.True(query.IsEmpty);
            Assert.Equal(0, query.TermCount);
            Assert.Null(query.PrefixTerm);
        }

        /// <summary>
        /// The single most important case, because it is what is true on nearly every keystroke:
        /// one partial word, which must be matched as a prefix or nothing appears until the user
        /// finishes typing.
        /// </summary>
        [Fact]
        public void A_single_partial_word_is_a_prefix_term()
        {
            SearchQuery query = SearchQuery.Parse("son");

            Assert.Empty(query.CompleteTerms);
            Assert.Equal("son", query.PrefixTerm);
            Assert.Equal(1, query.TermCount);
            Assert.False(query.IsEmpty);
        }

        /// <summary>
        /// Typing a space is how the user says "I have finished that word". The term stops being
        /// a prefix and starts being matched exactly.
        /// </summary>
        [Fact]
        public void A_trailing_space_completes_the_term()
        {
            SearchQuery query = SearchQuery.Parse("sonic ");

            Assert.Equal(new[] { "sonic" }, query.CompleteTerms);
            Assert.Null(query.PrefixTerm);
            Assert.Equal(1, query.TermCount);
        }

        [Fact]
        public void Only_the_last_term_of_several_is_a_prefix()
        {
            SearchQuery query = SearchQuery.Parse("sonic hedge");

            Assert.Equal(new[] { "sonic" }, query.CompleteTerms);
            Assert.Equal("hedge", query.PrefixTerm);
            Assert.Equal(2, query.TermCount);
        }

        [Fact]
        public void Several_completed_terms_are_all_exact()
        {
            SearchQuery query = SearchQuery.Parse("the legend of zelda ");

            Assert.Equal(new[] { "the", "legend", "of", "zelda" }, query.CompleteTerms);
            Assert.Null(query.PrefixTerm);
            Assert.Equal(4, query.TermCount);
        }

        /// <summary>
        /// The buffer is analysed exactly as titles are, so a query and a title that look the
        /// same to a person look the same to the engine.
        /// </summary>
        [Fact]
        public void The_query_is_analysed_the_same_way_a_title_is()
        {
            SearchQuery query = SearchQuery.Parse("Pokémon Red & Blue");

            Assert.Equal(new[] { "pokemon", "red", "and" }, query.CompleteTerms);
            Assert.Equal("blue", query.PrefixTerm);
        }

        /// <summary>
        /// An apostrophe continues a word rather than ending one, so a buffer sitting on one is
        /// still mid-term.
        /// </summary>
        [Fact]
        public void An_apostrophe_does_not_complete_a_term()
        {
            SearchQuery query = SearchQuery.Parse("robotnik'");

            Assert.Equal("robotnik", query.PrefixTerm);
            Assert.Empty(query.CompleteTerms);
        }

        [Fact]
        public void Leading_whitespace_does_not_create_an_empty_term()
        {
            SearchQuery query = SearchQuery.Parse("   sonic");

            Assert.Empty(query.CompleteTerms);
            Assert.Equal("sonic", query.PrefixTerm);
        }

        [Fact]
        public void Repeated_spaces_between_terms_are_collapsed()
        {
            SearchQuery query = SearchQuery.Parse("sonic    the   hedge");

            Assert.Equal(new[] { "sonic", "the" }, query.CompleteTerms);
            Assert.Equal("hedge", query.PrefixTerm);
        }

        /// <summary>
        /// Punctuation ends the term the user was typing, because the analyser turns it into a
        /// word break.
        /// </summary>
        [Fact]
        public void Punctuation_completes_the_term_before_it()
        {
            SearchQuery query = SearchQuery.Parse("zelda:");

            Assert.Equal(new[] { "zelda" }, query.CompleteTerms);
            Assert.Null(query.PrefixTerm);
        }

        [Fact]
        public void The_empty_query_is_shared_and_matches_nothing()
        {
            Assert.True(SearchQuery.Empty.IsEmpty);
            Assert.Same(SearchQuery.Empty, SearchQuery.Parse(""));
        }
    }
}
