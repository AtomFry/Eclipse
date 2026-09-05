using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-011 - the ranking table of docs/plans/text-search.md 5.5.
    ///
    /// These pin relationships rather than magic numbers wherever they can: "the short title
    /// beats the long one" survives tuning, "the score is 128.125" does not. The few tests that
    /// do assert a number are the ones where the number IS the rule - a whole prefix scoring the
    /// same as an exact match, for instance.
    /// </summary>
    public class SearchScoringTests
    {
        private static SearchableGame Game(string title, int popularity = 0)
        {
            return new SearchableGame(0, title, popularity);
        }

        private static double Score(string title, string buffer, int popularity = 0)
        {
            return SearchScoring.Score(Game(title, popularity), SearchQuery.Parse(buffer));
        }

        // ------------------------------------------------------------------
        // The AND. Every query term must match.
        // ------------------------------------------------------------------

        [Fact]
        public void A_game_matching_every_term_scores_above_zero()
        {
            Assert.True(Score("Sonic the Hedgehog", "sonic hedge") > 0);
        }

        [Fact]
        public void A_game_missing_any_term_scores_zero()
        {
            Assert.Equal(0, Score("Sonic the Hedgehog", "sonic mario"));
        }

        /// <summary>
        /// VER-SEARCH-013. Terms narrow; they never widen. Adding a term that the game does not
        /// carry takes it from matching to not matching.
        /// </summary>
        [Fact]
        public void Adding_a_term_narrows_rather_than_widens()
        {
            Assert.True(Score("Sonic the Hedgehog", "sonic ") > 0);
            Assert.Equal(0, Score("Sonic the Hedgehog", "sonic zelda"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void An_empty_query_scores_zero(string buffer)
        {
            Assert.Equal(0, Score("Sonic the Hedgehog", buffer));
        }

        [Fact]
        public void A_null_game_or_query_scores_zero()
        {
            Assert.Equal(0, SearchScoring.Score(null, SearchQuery.Parse("sonic")));
            Assert.Equal(0, SearchScoring.Score(Game("Sonic"), null));
        }

        // ------------------------------------------------------------------
        // Exact and prefix.
        // ------------------------------------------------------------------

        /// <summary>
        /// A longer prefix of a term is a better match, so the score climbs with every keystroke
        /// the user commits.
        /// </summary>
        [Fact]
        public void A_longer_prefix_scores_higher_than_a_shorter_one()
        {
            double s = Score("Sonic the Hedgehog", "s");
            double so = Score("Sonic the Hedgehog", "so");
            double son = Score("Sonic the Hedgehog", "son");
            double sonic = Score("Sonic the Hedgehog", "sonic");

            Assert.True(so > s);
            Assert.True(son > so);
            Assert.True(sonic > son);
        }

        /// <summary>
        /// A prefix covering the whole term scores exactly what an exact match scores. This is
        /// what stops the ranking jumping on the keystroke that finishes a word - the term has
        /// not become a better match, it has only stopped growing.
        /// </summary>
        [Fact]
        public void A_prefix_covering_the_whole_term_scores_as_an_exact_match()
        {
            Assert.Equal(SearchScoring.ExactTermScore,
                         SearchScoring.PrefixTermBaseScore + SearchScoring.PrefixTermLengthBonus);

            Assert.Equal(Score("Sonic the Hedgehog", "sonic "), Score("Sonic the Hedgehog", "sonic"), 6);
        }

        // ------------------------------------------------------------------
        // The bonuses.
        // ------------------------------------------------------------------

        /// <summary>
        /// The plan's worked example: matching the start of a title beats matching a word
        /// buried in it.
        /// </summary>
        [Fact]
        public void Matching_the_start_of_the_title_beats_matching_later_in_it()
        {
            double atStart = Score("Hedgehog Launch", "hedgehog");
            double laterOn = Score("Sonic the Hedgehog", "hedgehog");

            Assert.True(atStart > laterOn);
        }

        /// <summary>
        /// The plan's other worked example: the base game beats the collection.
        /// </summary>
        [Fact]
        public void A_short_title_beats_a_long_one_for_the_same_match()
        {
            double baseGame = Score("Sonic the Hedgehog 2", "sonic");
            double collection = Score("Sonic the Hedgehog 2 Special Edition Collection", "sonic");

            Assert.True(baseGame > collection);
        }

        /// <summary>
        /// Brevity stops helping once a title is long enough - otherwise a very long title would
        /// keep being penalised for length it has already been penalised for.
        /// </summary>
        [Fact]
        public void The_brevity_bonus_bottoms_out_at_a_long_title()
        {
            double eight = Score("Sonic a b c d e f g", "sonic");
            double twelve = Score("Sonic a b c d e f g h i j k", "sonic");

            Assert.Equal(eight, twelve, 6);
        }

        [Fact]
        public void A_more_popular_game_outranks_an_identical_less_popular_one()
        {
            double popular = Score("Sonic the Hedgehog", "sonic", 5);
            double obscure = Score("Sonic the Hedgehog", "sonic", 0);

            Assert.True(popular > obscure);
            Assert.Equal(SearchableGame.MaxPopularity, popular - obscure, 6);
        }

        /// <summary>
        /// Popularity is a tiebreak, not a ranking. It must not lift a poor match above a good
        /// one - the whole popularity range is smaller than the gap between match qualities.
        /// </summary>
        [Fact]
        public void Popularity_cannot_lift_a_worse_match_above_a_better_one()
        {
            double goodMatchUnpopular = Score("Sonic the Hedgehog", "sonic", 0);
            double poorMatchPopular = Score("Supersonic Acrobatic Rocket Powered Battle Cars", "so", 5);

            Assert.True(goodMatchUnpopular > poorMatchPopular);
        }

        // ------------------------------------------------------------------
        // Numeral alternates have to survive into scoring, or a game found through
        // one would score zero for the term that found it.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("vii")]
        [InlineData("7")]
        [InlineData("seven")]
        public void A_game_found_through_a_numeral_alternate_still_scores(string spelling)
        {
            Assert.True(Score("Final Fantasy VII", "final fantasy " + spelling) > 0);
        }

        [Fact]
        public void Every_spelling_of_a_number_scores_the_same()
        {
            double roman = Score("Final Fantasy VII", "final fantasy vii");
            double digit = Score("Final Fantasy VII", "final fantasy 7");

            Assert.Equal(roman, digit, 6);
        }

        // ------------------------------------------------------------------
        // Multi-term behaviour.
        // ------------------------------------------------------------------

        /// <summary>
        /// Scores are averaged across terms rather than summed, so a two-term query stays on the
        /// same scale as a one-term query and a person reading a failing test can still tell
        /// roughly what happened.
        /// </summary>
        [Fact]
        public void Term_scores_are_averaged_so_more_terms_does_not_mean_a_bigger_number()
        {
            double oneTerm = Score("Sonic the Hedgehog", "sonic ");
            double twoTerms = Score("Sonic the Hedgehog", "sonic hedgehog ");

            Assert.Equal(oneTerm, twoTerms, 6);
        }

        [Fact]
        public void Word_order_does_not_matter()
        {
            double inOrder = Score("Sonic the Hedgehog", "sonic hedgehog ");
            double reversed = Score("Sonic the Hedgehog", "hedgehog sonic ");

            Assert.Equal(inOrder, reversed, 6);
        }

        /// <summary>
        /// The first-token bonus is earned if any term matched the title's first word, which is
        /// what makes word order not matter for it either.
        /// </summary>
        [Fact]
        public void The_first_token_bonus_is_earned_whichever_term_matched_it()
        {
            double firstTermMatchesStart = Score("Sonic the Hedgehog", "sonic hedgehog ");
            double lastTermMatchesStart = Score("Sonic the Hedgehog", "hedgehog sonic ");

            Assert.Equal(firstTermMatchesStart, lastTermMatchesStart, 6);
        }
    }
}
