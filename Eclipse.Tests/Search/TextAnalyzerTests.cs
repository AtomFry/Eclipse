using System.Collections.Generic;
using System.Linq;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-010 - the analysis chain of docs/plans/text-search.md 5.2.
    ///
    /// Everything downstream inherits whatever this does, so these are the assertions most worth
    /// having: a change here silently reshapes every posting list and every query.
    /// </summary>
    public class TextAnalyzerTests
    {
        [Theory]
        [InlineData("Sonic", "sonic")]
        [InlineData("SONIC", "sonic")]
        [InlineData("SoNiC", "sonic")]
        public void Text_is_folded_to_lower_case(string input, string expected)
        {
            Assert.Equal(expected, TextAnalyzer.Normalize(input));
        }

        [Theory]
        [InlineData("Pokémon", "pokemon")]
        [InlineData("Pokemon", "pokemon")]
        [InlineData("Ōkami", "okami")]
        [InlineData("Café", "cafe")]
        public void Accents_are_stripped_so_the_plain_spelling_finds_the_game(string input, string expected)
        {
            Assert.Equal(expected, TextAnalyzer.Normalize(input));
        }

        [Fact]
        public void An_ampersand_becomes_the_word_and()
        {
            Assert.Equal(new[] { "sonic", "and", "knuckles" }, TextAnalyzer.Tokenize("Sonic & Knuckles"));
        }

        [Fact]
        public void A_plus_becomes_the_word_plus()
        {
            Assert.Equal(new[] { "pokemon", "red", "plus", "blue" }, TextAnalyzer.Tokenize("Pokemon Red+Blue"));
        }

        /// <summary>
        /// The ordering rule of 5.2: an apostrophe is dropped without leaving a space, so the
        /// letters either side stay one word. "Rock n' Roll" is the worked example.
        /// </summary>
        [Fact]
        public void An_apostrophe_is_dropped_without_splitting_the_word()
        {
            Assert.Equal(new[] { "rock", "n", "roll", "racing" }, TextAnalyzer.Tokenize("Rock n' Roll Racing"));
            Assert.Equal(new[] { "dr", "robotniks", "mean", "bean", "machine" },
                         TextAnalyzer.Tokenize("Dr. Robotnik's Mean Bean Machine"));
        }

        [Theory]
        [InlineData("'")]
        [InlineData("’")]
        [InlineData("‘")]
        [InlineData("`")]
        public void Every_apostrophe_shape_is_dropped_the_same_way(string apostrophe)
        {
            Assert.Equal("robotniks", TextAnalyzer.Normalize("Robotnik" + apostrophe + "s"));
        }

        [Theory]
        [InlineData("The Legend of Zelda: A Link to the Past")]
        [InlineData("The Legend of Zelda - A Link to the Past")]
        [InlineData("The Legend of Zelda, A Link to the Past")]
        public void Punctuation_becomes_a_word_break(string title)
        {
            Assert.Equal(new[] { "the", "legend", "of", "zelda", "a", "link", "to", "the", "past" },
                         TextAnalyzer.Tokenize(title));
        }

        /// <summary>
        /// Article handling needs no special case. Matching is token based and therefore order
        /// independent, so "The Legend of Zelda" and the LaunchBox sort form "Legend of Zelda,
        /// The" analyse to the same set of tokens.
        /// </summary>
        [Fact]
        public void A_moved_article_analyses_to_the_same_tokens()
        {
            IReadOnlyList<string> display = TextAnalyzer.Tokenize("The Legend of Zelda");
            IReadOnlyList<string> sorted = TextAnalyzer.Tokenize("Legend of Zelda, The");

            Assert.Equal(display.OrderBy(t => t), sorted.OrderBy(t => t));
        }

        [Theory]
        [InlineData("   Sonic   the    Hedgehog  ", "sonic the hedgehog")]
        [InlineData("Sonic---the---Hedgehog", "sonic the hedgehog")]
        [InlineData("!!!Sonic!!!", "sonic")]
        public void Whitespace_is_collapsed_and_trimmed(string input, string expected)
        {
            Assert.Equal(expected, TextAnalyzer.Normalize(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("!!!")]
        public void Text_with_nothing_in_it_yields_nothing(string input)
        {
            Assert.Equal(string.Empty, TextAnalyzer.Normalize(input));
            Assert.Empty(TextAnalyzer.Tokenize(input));
        }

        // ------------------------------------------------------------------
        // Numeral folding - additive, and index-time only.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("vii")]
        [InlineData("7")]
        [InlineData("seven")]
        public void Every_spelling_of_a_number_indexes_as_all_of_them(string spelling)
        {
            IReadOnlyList<string> terms = TextAnalyzer.IndexTerms(spelling);

            Assert.Contains("7", terms);
            Assert.Contains("vii", terms);
            Assert.Contains("seven", terms);
        }

        /// <summary>
        /// Additive, not destructive. This is the difference from RULE-SEARCH-004: the voice
        /// path replaces "vii" with "7", so the roman form stops being findable. Here the token
        /// is always among its own index terms.
        /// </summary>
        [Theory]
        [InlineData("ii")]
        [InlineData("x")]
        [InlineData("20")]
        [InlineData("zelda")]
        public void A_token_is_always_among_its_own_index_terms(string token)
        {
            Assert.Contains(token, TextAnalyzer.IndexTerms(token));
        }

        /// <summary>
        /// The consequence of additive folding that RULE-SEARCH-004 could not have: X is folded
        /// too. The voice path excludes it because replacing it would remove the letter; here
        /// nothing is removed, so "Final Fantasy X" is reachable by both "x" and "10".
        ///
        /// The cost is visible in the corpus tests: "Mega Man X" also becomes reachable by "10",
        /// which is not what its title means. Additive folding accepts that trade.
        /// </summary>
        [Fact]
        public void The_roman_numeral_X_is_folded_because_folding_is_additive()
        {
            IReadOnlyList<string> terms = TextAnalyzer.IndexTerms("x");

            Assert.Contains("x", terms);
            Assert.Contains("10", terms);
            Assert.Contains("ten", terms);
        }

        /// <summary>
        /// One is the exception. "i" is the English word far more often than the numeral, so
        /// folding it would make every title containing "I" findable by "one".
        /// </summary>
        [Fact]
        public void The_roman_numeral_one_is_not_folded()
        {
            Assert.Equal(new[] { "i" }, TextAnalyzer.IndexTerms("i"));

            IReadOnlyList<string> one = TextAnalyzer.IndexTerms("1");
            Assert.Contains("1", one);
            Assert.Contains("one", one);
            Assert.DoesNotContain("i", one);
        }

        [Theory]
        [InlineData("zelda")]
        [InlineData("sonic")]
        [InlineData("1997")]
        [InlineData("64")]
        public void A_token_that_is_not_a_small_number_indexes_as_itself_alone(string token)
        {
            Assert.Equal(new[] { token }, TextAnalyzer.IndexTerms(token));
        }

        [Fact]
        public void Numerals_are_folded_only_up_to_twenty()
        {
            Assert.Equal(3, TextAnalyzer.IndexTerms("20").Count);
            Assert.Equal(new[] { "21" }, TextAnalyzer.IndexTerms("21"));
        }

        /// <summary>
        /// The cheap probe scoring uses, so the common case costs one dictionary lookup and no
        /// allocation.
        /// </summary>
        [Fact]
        public void The_numeral_probe_answers_false_for_ordinary_words()
        {
            IReadOnlyList<string> alternates;

            Assert.False(TextAnalyzer.TryGetNumeralAlternates("zelda", out alternates));
            Assert.Null(alternates);

            Assert.True(TextAnalyzer.TryGetNumeralAlternates("vii", out alternates));
            Assert.Contains("7", alternates);
        }

        [Fact]
        public void Index_terms_of_nothing_are_nothing()
        {
            Assert.Empty(TextAnalyzer.IndexTerms(null));
            Assert.Empty(TextAnalyzer.IndexTerms(string.Empty));
        }
    }
}
