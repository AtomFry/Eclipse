using System.Collections.Generic;
using System.Linq;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-019 - the golden corpus assertions of docs/plans/text-search.md 12.1.
    ///
    /// The unit tests each pin one component. These run the whole engine - analyser, index,
    /// query, scoring - over a library of realistically shaped titles, which is the only way the
    /// interactions between them show up.
    ///
    /// Assertions are about which games come back and in what order, never about scores. Scores
    /// are expected to move when the ranking constants are tuned; the orderings here are what
    /// tuning is supposed to preserve or deliberately improve.
    ///
    /// The two tests at the bottom marked RANKING OBSERVATION are the exception. They pin
    /// behaviour that is arguably wrong and is deliberately not being fixed yet - see their
    /// comments. When someone tunes the ranking, those are the tests that should change, and
    /// changing them should be a decision rather than an accident.
    /// </summary>
    public class SearchCorpusTests
    {
        private static IReadOnlyList<string> Search(string buffer, int maxResults = 10)
        {
            return SearchCorpus.Search(buffer, maxResults);
        }

        private static string Top(string buffer)
        {
            IReadOnlyList<string> found = Search(buffer, 1);
            return found.Count == 0 ? null : found[0];
        }

        // ------------------------------------------------------------------
        // The headline case: a few characters, typed on a d-pad, find the game.
        // ------------------------------------------------------------------

        /// <summary>
        /// Three letters is what this feature has to be worth. The base game outranks its
        /// sequels, its spin-offs and the collection that contains it.
        /// </summary>
        [Fact]
        public void Three_letters_put_the_right_game_first()
        {
            Assert.Equal("Sonic the Hedgehog", Top("son"));
            Assert.Equal("Sonic the Hedgehog", Top("sonic"));
        }

        /// <summary>
        /// The ranking must not lurch on the keystroke that finishes a word. "sonic" as a term
        /// still being typed and "sonic " as a completed one produce the same order.
        /// </summary>
        [Fact]
        public void Completing_a_word_does_not_reshuffle_the_results()
        {
            Assert.Equal(Search("sonic"), Search("sonic "));
        }

        /// <summary>
        /// Every keystroke narrows. This is the feedback loop the live result row exists to
        /// show, and it has to be monotonic for prefix matching or the user cannot tell whether
        /// they are getting warmer.
        /// </summary>
        [Fact]
        public void Each_extra_character_narrows_the_result_set()
        {
            int s = Search("s", 500).Count;
            int so = Search("so", 500).Count;
            int son = Search("son", 500).Count;
            int soni = Search("soni", 500).Count;

            Assert.True(s > so);
            Assert.True(so > son);
            Assert.True(son > soni);
        }

        /// <summary>
        /// Every game a single letter returns carries an index term starting with that letter.
        ///
        /// Asserted against index terms rather than against the words of the display title,
        /// because those are not the same set - a game can legitimately be reached through an
        /// alternate spelling that does not appear in its title at all. See the test below.
        /// </summary>
        [Fact]
        public void Every_game_a_single_letter_returns_carries_a_term_starting_with_it()
        {
            IReadOnlyList<string> found = Search("s", 500);
            Assert.NotEmpty(found);

            foreach (string title in found)
            {
                SearchableGame game = SearchCorpus.Games.Single(candidate => candidate.DisplayTitle == title);

                Assert.Contains(game.TitleTokens.SelectMany(TextAnalyzer.IndexTerms),
                                term => term.StartsWith("s", System.StringComparison.Ordinal));
            }
        }

        /// <summary>
        /// The consequence of indexing the word form of a number: "Final Fantasy VI" is
        /// reachable by "six", and therefore by "s", even though neither string appears in its
        /// title. That is the point - a user who knows the game as "Final Fantasy Six" can type
        /// what they say rather than what is printed - but it does mean a broad prefix reaches
        /// further than the display titles suggest.
        /// </summary>
        [Fact]
        public void A_numeral_is_reachable_through_the_prefix_of_its_spelled_out_form()
        {
            Assert.Equal(new[] { "Final Fantasy VI" }, Search("final fantasy six "));
            Assert.Contains("Final Fantasy VI", Search("final fantasy si"));
        }

        // ------------------------------------------------------------------
        // Multi-term AND.
        // ------------------------------------------------------------------

        /// <summary>
        /// Two words the user remembers from a long title, in neither their original order nor
        /// adjacent to each other, find exactly one game.
        /// </summary>
        [Fact]
        public void Two_remembered_words_find_one_game_out_of_a_series()
        {
            Assert.Equal(new[] { "The Legend of Zelda: A Link to the Past" }, Search("link past"));
            Assert.Equal(new[] { "The Legend of Zelda: A Link to the Past" }, Search("past link"));
        }

        [Fact]
        public void A_two_word_query_narrows_a_large_family()
        {
            Assert.True(Search("super", 500).Count > Search("super mario", 500).Count);
            Assert.All(Search("super mario", 500), title => Assert.Contains("Super Mario", title));
        }

        [Fact]
        public void Words_the_library_does_not_pair_find_nothing()
        {
            Assert.Empty(Search("sonic zelda"));
            Assert.Empty(Search("mario castlevania"));
        }

        // ------------------------------------------------------------------
        // The analysis chain, end to end.
        // ------------------------------------------------------------------

        [Fact]
        public void Accented_titles_are_found_by_their_plain_spelling()
        {
            IReadOnlyList<string> found = Search("pokemon");

            Assert.Equal(3, found.Count);
            Assert.All(found, title => Assert.StartsWith("Pokémon", title));
        }

        [Fact]
        public void A_macron_is_folded_like_any_other_accent()
        {
            Assert.Equal("Ōkami", Top("okami"));
        }

        [Fact]
        public void An_apostrophe_does_not_break_the_word_around_it()
        {
            Assert.Equal("Rock n' Roll Racing", Top("rock n roll"));
            Assert.Equal("Dr. Robotnik's Mean Bean Machine", Top("robotniks"));
        }

        [Fact]
        public void An_ampersand_can_be_typed_as_the_word_and()
        {
            Assert.Equal("Sonic & Knuckles", Top("sonic and knuckles"));
        }

        /// <summary>
        /// The on-screen keyboard has no ampersand key, so this is the only way a user can
        /// reach that title by its full name.
        /// </summary>
        [Fact]
        public void A_title_with_an_ampersand_is_reachable_without_typing_one()
        {
            Assert.Contains("Sonic & Knuckles", Search("knuckles"));
        }

        [Fact]
        public void Both_halves_of_a_slash_separated_title_are_searchable()
        {
            const string title = "Sonic 3D Blast / Sonic 3D Flickies' Island";

            Assert.Contains(title, Search("blast"));
            Assert.Contains(title, Search("flickies"));
        }

        // ------------------------------------------------------------------
        // Numerals - the improvement over the voice path's destructive substitution.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("final fantasy vii ")]
        [InlineData("final fantasy 7 ")]
        [InlineData("final fantasy seven ")]
        public void A_numbered_sequel_is_found_by_any_spelling_of_its_number(string buffer)
        {
            Assert.Equal(new[] { "Final Fantasy VII" }, Search(buffer));
        }

        /// <summary>
        /// The whole point of folding being additive: the roman form printed on the box stays
        /// findable, which under RULE-SEARCH-004's destructive substitution it would not be.
        /// </summary>
        [Fact]
        public void The_spelling_printed_on_the_box_still_finds_the_game()
        {
            Assert.Contains("Castlevania III: Dracula's Curse", Search("castlevania iii "));
            Assert.Contains("Castlevania III: Dracula's Curse", Search("castlevania 3 "));
        }

        [Fact]
        public void A_digit_in_a_title_is_findable_by_its_roman_and_word_forms()
        {
            Assert.Contains("Sonic the Hedgehog 2", Search("sonic ii "));
            Assert.Contains("Sonic the Hedgehog 2", Search("sonic two "));
        }

        [Fact]
        public void Super_Castlevania_IV_is_found_by_typing_four()
        {
            Assert.Equal("Super Castlevania IV", Top("castlevania 4"));
        }

        // ------------------------------------------------------------------
        // Known gaps. These assert that Stage 1 does NOT do something, so that the
        // stage which adds it has a test that changes from empty to useful.
        // ------------------------------------------------------------------

        /// <summary>
        /// GAP - fuzzy matching is stage 3. Until then a typo finds nothing. This is the single
        /// assertion that should flip when FuzzyMatcher lands: "sonik" must find Sonic.
        /// </summary>
        [Fact]
        public void A_typo_finds_nothing_until_fuzzy_matching_arrives()
        {
            Assert.Empty(Search("sonik"));
            Assert.Empty(Search("castlevaina"));
        }

        /// <summary>
        /// GAP - acronyms are not in the plan at all. "ff7" is one token and the index holds
        /// words, so it matches nothing. Recorded because it appears in the plan's list of
        /// corpus queries and someone will otherwise assume it works.
        /// </summary>
        [Fact]
        public void An_acronym_is_not_expanded()
        {
            Assert.Empty(Search("ff7"));
            Assert.Equal(new[] { "Final Fantasy VII" }, Search("final fantasy 7 "));
        }

        // ------------------------------------------------------------------
        // RANKING OBSERVATIONS
        //
        // Behaviour that is arguably wrong, pinned rather than fixed. The plan is explicit that
        // the scoring constants are initial values to be tuned against a real library, and that
        // tuning is stage 5 - not something to do from a guess while building the engine.
        // ------------------------------------------------------------------

        /// <summary>
        /// The case the ranking rework exists for.
        ///
        /// "zelda" matches both "The Legend of Zelda" and "Zelda II: The Adventure of Link"
        /// exactly, and nothing in the *text* says which one the user meant - the sequel even
        /// has the better claim, since it starts with the word while the original has it fourth,
        /// behind an article.
        ///
        /// Under the original additive score the sequel won, because matching the first token
        /// was worth a flat fifteen points - more than the whole popularity range and the whole
        /// brevity range combined, so nothing could outvote it. The fix was not a better weight
        /// but a different shape: signals are compared in order of authority, so match quality
        /// decides first, then popularity, then brevity, and position only breaks a complete
        /// tie.
        ///
        /// Both of the lower signals point the right way here, which is why the answer is robust:
        /// popularity picks the original on a rated library, and brevity picks it on one with no
        /// ratings at all.
        /// </summary>
        [Fact]
        public void The_famous_game_wins_over_the_sequel_that_starts_with_the_word()
        {
            IReadOnlyList<string> found = Search("zelda");

            Assert.Equal("The Legend of Zelda", found[0]);
            Assert.Equal("Zelda II: The Adventure of Link", found[found.Count - 1]);
        }

        /// <summary>
        /// And the same while the word is still being typed, so the ordering does not lurch on
        /// the keystroke that completes it.
        /// </summary>
        [Fact]
        public void The_partial_query_ranks_the_same_way_as_the_finished_one()
        {
            Assert.Equal(Search("zel"), Search("zelda"));
        }

        /// <summary>
        /// The whole Zelda family in order: the original, then the other five-star entries by
        /// title length, then the four-star, then the three-star sequel. Popularity leads,
        /// brevity breaks its ties.
        /// </summary>
        [Fact]
        public void The_zelda_family_ranks_by_popularity_then_brevity()
        {
            Assert.Equal(new[]
            {
                "The Legend of Zelda",
                "The Legend of Zelda: Ocarina of Time",
                "The Legend of Zelda: A Link to the Past",
                "The Legend of Zelda: Majora's Mask",
                "Zelda II: The Adventure of Link"
            }, Search("zelda"));
        }

        /// <summary>
        /// RANKING OBSERVATION - the cost of folding X, accepted knowingly.
        ///
        /// Because numeral folding is additive, X indexes as ten - which makes "Final Fantasy X"
        /// reachable by typing 10, and also makes "Mega Man X" reachable by typing 10, where the
        /// X is a letter and not a number at all.
        ///
        /// The voice path avoids this by refusing to convert X (RULE-SEARCH-004), but it can
        /// only afford that because its substitution is destructive - converting would lose the
        /// letter. Additive folding trades this false positive for never losing a spelling. The
        /// plan takes that trade explicitly; this is what it costs.
        /// </summary>
        [Fact]
        public void Ranking_observation_folding_X_makes_a_letter_findable_as_a_number()
        {
            IReadOnlyList<string> found = Search("10");

            Assert.Contains("Final Fantasy X", found);
            Assert.Contains("Mega Man X", found);

            // and the letter still works, which is what the fold bought
            Assert.Equal("Mega Man X", Top("mega man x"));
        }

        // ------------------------------------------------------------------
        // Determinism.
        // ------------------------------------------------------------------

        [Fact]
        public void Identical_scores_break_ties_in_catalog_order()
        {
            // three titles of the same length and popularity, so only the tiebreak separates them
            IReadOnlyList<string> found = Search("super mario", 3);

            Assert.Equal(new[] { "Super Mario 64", "Super Mario Bros.", "Super Mario World" }, found);
        }

        [Fact]
        public void The_same_query_always_returns_the_same_order()
        {
            Assert.Equal(Search("s", 500), Search("s", 500));
            Assert.Equal(Search("sonic"), Search("sonic"));
        }

        [Fact]
        public void The_corpus_is_the_size_the_plan_asks_for()
        {
            // 12.1: 60-80 games, hand curated, every entry there for a stated reason
            Assert.InRange(SearchCorpus.Games.Count, 60, 80);
            Assert.Equal(SearchCorpus.Games.Count, SearchCorpus.Games.Select(g => g.CatalogIndex).Distinct().Count());
        }
    }
}
