using Eclipse.Models;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// VER-SEARCH-001, VER-SEARCH-002 and VER-SEARCH-003 - the characterization tests named as
    /// prerequisite P1 of docs/plans/text-search.md.
    ///
    /// These exist to protect behaviour that is about to acquire a neighbour. Text search
    /// introduces a second analysis chain over game titles, and the day someone proposes
    /// converging it with GameTitleGrammar, these assertions are the only thing standing between
    /// that refactor and a silent change to every voice search result. VERIFICATION.md ranks
    /// this 2nd and 3rd of everything uncovered and notes it needs nothing to write.
    ///
    /// They are characterization tests, not specifications: they assert what the code does
    /// today, including the parts that look accidental. Where behaviour is surprising it is
    /// called out rather than corrected - correcting it here would be a product change wearing
    /// a test's clothes.
    ///
    /// COVERAGE GAPS, deliberately left open in Stage 1
    /// ------------------------------------------------
    /// Two rules cannot be reached from this project, because Eclipse.Tests has no reference to
    /// Unbroken.LaunchBox.Plugins - the Eclipse project references it with Private=false, so it
    /// neither copies to output nor flows transitively. Every existing test in this project is
    /// LaunchBox-free for the same reason.
    ///
    ///   RULE-SEARCH-001  a title containing '/' splits into independent titles.
    ///                    Lives in GameTitleGrammarBuilder(IGame), which needs an IGame double.
    ///
    ///   RULE-SEARCH-005  every contiguous run of words is registered as a phrase.
    ///                    Lives in VoiceSearchIndex.BuildPhrasesForGame - private static, and it
    ///                    takes a GameMatch built around an IGame.
    ///
    /// Both would become reachable by adding a reference to the vendored contract assembly for
    /// this project only. That is safe - test output never reaches the deployed plugin folder,
    /// so RULE-INTEGRATE-011 is not in play - but it was out of scope for Stage 1 and is
    /// recorded here so the gap is visible rather than assumed covered.
    ///
    /// What IS covered below is the whole of the title decomposition that operates on strings,
    /// and the whole of the match scoring.
    /// </summary>
    public class VoiceSearchCharacterizationTests
    {
        // ------------------------------------------------------------------
        // RULE-SEARCH-002 - colon splits main title from subtitle, and the stored
        // title becomes the two joined by a space with the colon dropped.
        // ------------------------------------------------------------------

        [Fact]
        public void A_colon_splits_the_title_into_main_title_and_subtitle()
        {
            GameTitleGrammar grammar = new GameTitleGrammar("Final Fantasy VII: Remake");

            Assert.Equal("Final Fantasy VII", grammar.MainTitle);
            Assert.Equal("Remake", grammar.Subtitle);
        }

        /// <summary>
        /// The stored title is the two halves joined by a space - not the original string. The
        /// colon is gone and any spacing around it is normalised to exactly one space.
        /// </summary>
        [Fact]
        public void The_stored_title_is_the_two_halves_joined_by_a_space()
        {
            GameTitleGrammar grammar = new GameTitleGrammar("Final Fantasy VII: Remake");

            Assert.Equal("Final Fantasy VII Remake", grammar.Title);
        }

        [Fact]
        public void A_title_with_no_colon_keeps_its_title_and_has_no_subtitle()
        {
            GameTitleGrammar grammar = new GameTitleGrammar("Super Mario Bros");

            Assert.Equal("Super Mario Bros", grammar.Title);
            Assert.Null(grammar.MainTitle);
            Assert.Null(grammar.Subtitle);
        }

        /// <summary>
        /// The split needs the colon at index 1 or later. A title that opens with a colon is
        /// treated as having no split at all - IndexOf returns 0, and the guard is `>= 1`.
        /// </summary>
        [Fact]
        public void A_leading_colon_does_not_split_the_title()
        {
            GameTitleGrammar grammar = new GameTitleGrammar(":Odd");

            Assert.Null(grammar.MainTitle);
            Assert.Null(grammar.Subtitle);
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-003 - the characters : " ! are removed before word splitting.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("Half-Life: Opposing Force", "Half-Life Opposing Force")]
        [InlineData("Say \"Hello\"", "Say Hello")]
        [InlineData("Wario Land 4!", "Wario Land 4")]
        public void Colons_quotes_and_exclamation_marks_are_removed(string input, string expected)
        {
            Assert.Equal(expected, GameTitleGrammar.RemoveUnwantedCharacters(input));
        }

        /// <summary>
        /// Only those three. Apostrophes, hyphens, commas, ampersands and periods survive into
        /// the words, which is why "Sonic &amp; Knuckles" produces "&amp;" as a word of its own.
        /// </summary>
        [Fact]
        public void Other_punctuation_survives_into_the_title_words()
        {
            GameTitleGrammar grammar = new GameTitleGrammar("Sonic & Knuckles");

            Assert.Equal(new[] { "Sonic", "&", "Knuckles" }, grammar.TitleWords);
        }

        [Fact]
        public void Removing_unwanted_characters_from_nothing_yields_an_empty_string()
        {
            Assert.Equal(string.Empty, GameTitleGrammar.RemoveUnwantedCharacters(null));
            Assert.Equal(string.Empty, GameTitleGrammar.RemoveUnwantedCharacters("   "));
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-004 - roman numerals II..XIX become digits, and X is
        // deliberately excluded.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("II", "2")]
        [InlineData("III", "3")]
        [InlineData("IV", "4")]
        [InlineData("V", "5")]
        [InlineData("VI", "6")]
        [InlineData("VII", "7")]
        [InlineData("VIII", "8")]
        [InlineData("IX", "9")]
        [InlineData("XI", "11")]
        [InlineData("XII", "12")]
        [InlineData("XIII", "13")]
        [InlineData("XIV", "14")]
        [InlineData("XV", "15")]
        [InlineData("XVI", "16")]
        [InlineData("XVII", "17")]
        [InlineData("XVIII", "18")]
        [InlineData("XIX", "19")]
        public void Roman_numerals_are_replaced_with_digits(string numeral, string expected)
        {
            Assert.Equal(expected, GameTitleGrammar.GetRomanNumeralReplacement(numeral));
        }

        /// <summary>
        /// VER-SEARCH-002. X is not converted, because too many titles contain a literal X -
        /// the code says so in a comment that apologises to Final Fantasy X.
        /// </summary>
        [Fact]
        public void The_roman_numeral_X_is_deliberately_not_converted()
        {
            Assert.Equal("X", GameTitleGrammar.GetRomanNumeralReplacement("X"));

            GameTitleGrammar grammar = new GameTitleGrammar("Final Fantasy X");

            Assert.Equal(new[] { "Final", "Fantasy", "X" }, grammar.TitleWords);
        }

        /// <summary>
        /// The replacement is case sensitive and matches the whole word - a lower case numeral
        /// is left alone. Characterization: this looks accidental rather than intended, but it
        /// is what ships.
        /// </summary>
        [Fact]
        public void Roman_numeral_replacement_is_case_sensitive()
        {
            Assert.Equal("vii", GameTitleGrammar.GetRomanNumeralReplacement("vii"));
        }

        [Fact]
        public void A_word_that_is_not_a_numeral_is_returned_unchanged()
        {
            Assert.Equal("Zelda", GameTitleGrammar.GetRomanNumeralReplacement("Zelda"));
        }

        /// <summary>
        /// VER-SEARCH-001, the whole of it that is reachable from here: the worked example from
        /// VERIFICATION.md. Colon split, colon removed, numeral converted, words in order.
        /// </summary>
        [Fact]
        public void The_verification_example_decomposes_as_documented()
        {
            GameTitleGrammar grammar = new GameTitleGrammar("Final Fantasy VII: Remake");

            Assert.Equal("Final Fantasy VII", grammar.MainTitle);
            Assert.Equal("Remake", grammar.Subtitle);
            Assert.Equal("Final Fantasy VII Remake", grammar.Title);
            Assert.Equal(new[] { "Final", "Fantasy", "7", "Remake" }, grammar.TitleWords);
        }

        /// <summary>
        /// The words come from both halves, in order - main title first, then subtitle. Note
        /// the asymmetry this creates and that the test above already shows: TitleWords holds
        /// the converted numeral "7", while Title still holds the original "VII", because the
        /// numeral replacement is applied to the local working copy of the string rather than
        /// to the stored Title. Characterization, not endorsement.
        /// </summary>
        [Fact]
        public void The_stored_title_keeps_the_roman_numeral_that_the_words_converted()
        {
            GameTitleGrammar grammar = new GameTitleGrammar("Final Fantasy VII: Remake");

            Assert.Contains("VII", grammar.Title);
            Assert.Contains("7", grammar.TitleWords);
            Assert.DoesNotContain("VII", grammar.TitleWords);
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-006 and RULE-SEARCH-007 - the noise word list, and the fact
        // that it is applied to the accumulated phrase rather than to single words.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("the")]
        [InlineData("a")]
        [InlineData("of")]
        [InlineData("at")]
        [InlineData("as")]
        [InlineData("and")]
        [InlineData("to")]
        [InlineData("n'")]
        [InlineData("'n")]
        [InlineData("b")]
        [InlineData("in")]
        [InlineData("on")]
        public void The_noise_words_are_the_twelve_that_ship(string word)
        {
            Assert.True(GameTitleGrammar.IsNoiseWord(word));
        }

        [Fact]
        public void Noise_word_matching_ignores_case()
        {
            Assert.True(GameTitleGrammar.IsNoiseWord("The"));
            Assert.True(GameTitleGrammar.IsNoiseWord("AND"));
        }

        /// <summary>
        /// RULE-SEARCH-007. The test is applied to the whole accumulated phrase, so a phrase
        /// that merely starts with a noise word is registered - "the legend" survives while
        /// "the" alone does not. This is the rule that keeps partial phrases useful.
        /// </summary>
        [Fact]
        public void A_phrase_beginning_with_a_noise_word_is_not_itself_noise()
        {
            Assert.True(GameTitleGrammar.IsNoiseWord("the"));
            Assert.False(GameTitleGrammar.IsNoiseWord("the legend"));
            Assert.False(GameTitleGrammar.IsNoiseWord("a link to the past"));
        }

        [Theory]
        [InlineData("zelda")]
        [InlineData("mario")]
        [InlineData("an")]
        [InlineData("")]
        public void Ordinary_words_are_not_noise(string word)
        {
            Assert.False(GameTitleGrammar.IsNoiseWord(word));
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-010, 011, 012, 017 - match scoring. VER-SEARCH-003.
        //
        // GameMatch's parameterless constructor leaves Game null, and the scoring path never
        // touches it - so the whole of the scoring rule set is reachable without an IGame.
        // ------------------------------------------------------------------

        private static GameMatch Scored(TitleMatchType matchType, string convertedTitle, string phrase, float confidence)
        {
            GameMatch gameMatch = new GameMatch
            {
                TitleMatchType = matchType,
                ConvertedTitle = convertedTitle
            };

            gameMatch.SetupVoiceMatchPercentage(confidence, phrase);
            return gameMatch;
        }

        /// <summary>
        /// RULE-SEARCH-010 base scores, and RULE-SEARCH-011's "no headroom" case: a whole title
        /// match starts at 100, so the length bonus has nothing to award.
        /// </summary>
        [Fact]
        public void A_whole_title_match_at_full_confidence_scores_99()
        {
            GameMatch gameMatch = Scored(TitleMatchType.FullTitleMatch, "super mario bros", "super mario bros", 1.0f);

            Assert.Equal(99, gameMatch.MatchPercentage);
        }

        /// <summary>
        /// RULE-SEARCH-012 - 0.001 is subtracted so the score can never reach exactly 100. This
        /// is the assertion that rule exists for.
        /// </summary>
        [Theory]
        [InlineData(TitleMatchType.FullTitleMatch)]
        [InlineData(TitleMatchType.MainTitleMatch)]
        [InlineData(TitleMatchType.SubtitleMatch)]
        [InlineData(TitleMatchType.FullTitleContains)]
        public void No_match_type_can_reach_one_hundred_percent(TitleMatchType matchType)
        {
            GameMatch gameMatch = Scored(matchType, "super mario bros", "super mario bros", 1.0f);

            Assert.True(gameMatch.MatchPercentage < 100);
        }

        /// <summary>
        /// RULE-SEARCH-011 - the worked example from the comment in SetupVoiceMatchPercentage:
        /// "super mario" (11 chars) against "super mario bros." (17 chars) as a contained
        /// fragment. Base 60, headroom 40, awarded in proportion 11/17, giving 85.
        /// </summary>
        [Fact]
        public void A_contained_fragment_earns_headroom_in_proportion_to_the_title_it_covered()
        {
            GameMatch gameMatch = Scored(TitleMatchType.FullTitleContains, "super mario bros.", "super mario", 1.0f);

            Assert.Equal(85, gameMatch.MatchPercentage);
        }

        /// <summary>
        /// RULE-SEARCH-010 - the four base scores are ordered whole title, main title, subtitle,
        /// fragment, and that order survives scoring when everything else is held equal.
        /// </summary>
        [Fact]
        public void The_match_types_score_in_their_documented_order()
        {
            const string title = "the legend of zelda";

            int whole = Scored(TitleMatchType.FullTitleMatch, title, "zelda", 1.0f).MatchPercentage;
            int main = Scored(TitleMatchType.MainTitleMatch, title, "zelda", 1.0f).MatchPercentage;
            int subtitle = Scored(TitleMatchType.SubtitleMatch, title, "zelda", 1.0f).MatchPercentage;
            int fragment = Scored(TitleMatchType.FullTitleContains, title, "zelda", 1.0f).MatchPercentage;

            Assert.True(whole > main);
            Assert.True(main > subtitle);
            Assert.True(subtitle > fragment);
        }

        /// <summary>
        /// RULE-SEARCH-012 - the whole score is multiplied by the recogniser's confidence, so
        /// half confidence is half the score.
        /// </summary>
        [Fact]
        public void Confidence_multiplies_the_whole_score()
        {
            int full = Scored(TitleMatchType.FullTitleMatch, "super mario bros", "super mario bros", 1.0f).MatchPercentage;
            int half = Scored(TitleMatchType.FullTitleMatch, "super mario bros", "super mario bros", 0.5f).MatchPercentage;

            Assert.Equal(99, full);
            Assert.Equal(49, half);
        }

        /// <summary>
        /// RULE-SEARCH-017 - the score is displayed as a two digit percentage, zero padded.
        /// </summary>
        [Fact]
        public void The_match_description_is_a_two_digit_percentage()
        {
            GameMatch high = Scored(TitleMatchType.FullTitleContains, "super mario bros.", "super mario", 1.0f);
            Assert.Equal("85% Match", high.MatchDescription);

            GameMatch low = Scored(TitleMatchType.FullTitleContains, "the legend of zelda", "of", 0.1f);
            Assert.Equal("06% Match", low.MatchDescription);
        }

        /// <summary>
        /// A phrase longer than the title it matched awards more than the available headroom,
        /// because the proportion is not clamped to 1. It cannot arise from the voice path,
        /// which only ever matches phrases drawn from the title itself, but it is what the
        /// arithmetic does and a rewrite should know that before "fixing" it.
        /// </summary>
        [Fact]
        public void A_phrase_longer_than_its_title_is_not_clamped()
        {
            GameMatch gameMatch = Scored(TitleMatchType.FullTitleContains, "sonic", "sonic the hedgehog", 1.0f);

            Assert.True(gameMatch.MatchPercentage > 100);
        }
    }
}
