using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Tests.Fakes;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// The two voice decomposition rules that need a whole game rather than a title, and were
    /// therefore left uncovered when the rest of VER-SEARCH-001 was written.
    ///
    /// `VoiceSearchCharacterizationTests` records why: `Eclipse.Tests` had no reference to the
    /// LaunchBox contract assembly, so anything taking an IGame was unreachable. That reference
    /// now exists for tests alone, and these are the assertions it was blocking.
    ///
    /// Characterization, like the rest: what the code does today, including the parts that look
    /// accidental. `B-30` would rewrite this decomposition, and these say what it must preserve.
    /// </summary>
    public class VoiceSearchGrammarTests
    {
        private static GameTitleGrammarBuilder GrammarFor(string title)
        {
            return new GameTitleGrammarBuilder(FakeGame.With("1", title));
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-001 - a title containing '/' becomes several titles.
        // ------------------------------------------------------------------

        [Fact]
        public void A_title_with_no_slash_produces_one_grammar()
        {
            Assert.Single(GrammarFor("Sonic the Hedgehog").gameTitleGrammars);
        }

        /// <summary>
        /// The regional-variant shape LaunchBox actually produces, and the reason this rule
        /// exists: both names have to be speakable.
        /// </summary>
        [Fact]
        public void A_slash_splits_the_title_into_independent_titles()
        {
            List<GameTitleGrammar> grammars = GrammarFor("Sonic 3D Blast / Sonic 3D Flickies' Island").gameTitleGrammars;

            Assert.Equal(2, grammars.Count);
            Assert.Equal("Sonic 3D Blast", grammars[0].Title);
            Assert.Equal("Sonic 3D Flickies' Island", grammars[1].Title);
        }

        [Fact]
        public void Each_half_is_decomposed_on_its_own()
        {
            List<GameTitleGrammar> grammars = GrammarFor("Contra / Probotector").gameTitleGrammars;

            Assert.Equal(new[] { "Contra" }, grammars[0].TitleWords);
            Assert.Equal(new[] { "Probotector" }, grammars[1].TitleWords);
        }

        [Fact]
        public void Whitespace_around_a_slash_is_trimmed()
        {
            Assert.Equal("Probotector", GrammarFor("Contra   /   Probotector").gameTitleGrammars[1].Title);
        }

        [Fact]
        public void More_than_one_slash_produces_more_than_two_titles()
        {
            Assert.Equal(3, GrammarFor("A / B / C").gameTitleGrammars.Count);
        }

        /// <summary>
        /// Empty entries are dropped rather than producing a grammar with no words, so a
        /// trailing or doubled slash costs nothing.
        /// </summary>
        [Theory]
        [InlineData("Contra /")]
        [InlineData("Contra //")]
        [InlineData("/ Contra")]
        public void An_empty_half_is_not_a_title(string title)
        {
            Assert.Single(GrammarFor(title).gameTitleGrammars);
        }

        /// <summary>
        /// The two rules compose: each half of a slash is then split on its own colon.
        /// </summary>
        [Fact]
        public void A_half_containing_a_colon_still_splits_into_main_title_and_subtitle()
        {
            GameTitleGrammar second = GrammarFor("Ys III / Ys III: Wanderers from Ys").gameTitleGrammars[1];

            Assert.Equal("Ys III", second.MainTitle);
            Assert.Equal("Wanderers from Ys", second.Subtitle);
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-005 and 007 - every contiguous run of words is registered,
        // with noise-word filtering applied to the accumulated phrase.
        //
        // The registration itself lives in VoiceSearchIndex.BuildPhrasesForGame, which is
        // private and reachable only through the singleton's whole-library build. What is
        // asserted here is the input that rule consumes - the word list every run is taken from -
        // together with the filtering rule that decides which runs survive.
        // ------------------------------------------------------------------

        /// <summary>The runs the rule produces, as it produces them.</summary>
        private static List<string> ContiguousRuns(string title)
        {
            List<string> words = GrammarFor(title).gameTitleGrammars[0].TitleWords;
            List<string> runs = new List<string>();

            for (int start = 0; start < words.Count; start++)
            {
                for (int end = start; end < words.Count; end++)
                {
                    string phrase = string.Join(" ", words.Skip(start).Take(end - start + 1));

                    if (!GameTitleGrammar.IsNoiseWord(phrase))
                    {
                        runs.Add(phrase);
                    }
                }
            }

            return runs;
        }

        [Fact]
        public void Every_contiguous_run_of_words_is_a_phrase()
        {
            Assert.Equal(new[]
            {
                "Super", "Super Mario", "Super Mario Bros",
                "Mario", "Mario Bros",
                "Bros"
            }, ContiguousRuns("Super Mario Bros"));
        }

        /// <summary>
        /// RULE-SEARCH-007 in the shape it matters: "the" alone is dropped, "the legend" is kept.
        /// Filtering the individual words instead would lose every phrase starting with an
        /// article - which is most of them.
        /// </summary>
        [Fact]
        public void A_run_beginning_with_a_noise_word_survives_while_the_word_alone_does_not()
        {
            List<string> runs = ContiguousRuns("The Legend of Zelda");

            Assert.DoesNotContain("The", runs);
            Assert.DoesNotContain("of", runs);
            Assert.Contains("The Legend", runs);
            Assert.Contains("of Zelda", runs);
            Assert.Contains("The Legend of Zelda", runs);
        }

        /// <summary>
        /// The fan-out `S-8` names, stated as a number: a title of n words produces n(n+1)/2 runs
        /// before noise filtering. Seven words is twenty-eight phrases, for one game.
        /// </summary>
        [Fact]
        public void The_number_of_runs_grows_with_the_square_of_the_title()
        {
            Assert.Single(ContiguousRuns("Tetris"));
            Assert.Equal(3, ContiguousRuns("Chrono Trigger").Count);
            Assert.Equal(6, ContiguousRuns("Super Mario Bros").Count);
            Assert.Equal(10, ContiguousRuns("Sonic 3D Blast Deluxe").Count);
        }

        /// <summary>
        /// And the runs are taken from the converted words, so a roman numeral is spoken as its
        /// digit - which is the destructive substitution the text search path deliberately does
        /// not share (RULE-SEARCH-048).
        /// </summary>
        [Fact]
        public void Runs_carry_the_converted_numeral_rather_than_the_printed_one()
        {
            List<string> runs = ContiguousRuns("Final Fantasy VII");

            Assert.Contains("Final Fantasy 7", runs);
            Assert.DoesNotContain("Final Fantasy VII", runs);
        }
    }
}
