using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-011 - the ranking rules of docs/plans/text-search.md 5.5, as reworked into an
    /// ordered comparison rather than a weighted sum.
    ///
    /// These pin relationships rather than magic numbers wherever they can: "the short title
    /// beats the long one" survives tuning, "the score is 128.125" does not. The few that do
    /// assert a number are the ones where the number IS the rule - a whole prefix scoring the
    /// same as an exact match, for instance.
    ///
    /// The most important tests here are the ones asserting that a weak signal <em>cannot</em>
    /// outvote a strong one. Under the old additive score that was a property of the chosen
    /// weights and could be broken by tuning them; it is now structural.
    /// </summary>
    public class SearchScoringTests
    {
        private static SearchableGame Game(string title, int popularity = 0)
        {
            return new SearchableGame(0, title, popularity);
        }

        private static SearchRank Rank(string title, string buffer, int popularity = 0)
        {
            return SearchScoring.Rank(Game(title, popularity), SearchQuery.Parse(buffer));
        }

        /// <summary>Whether the first ranks ahead of the second.</summary>
        private static bool Beats(SearchRank better, SearchRank worse)
        {
            return better.CompareTo(worse) > 0;
        }

        // ------------------------------------------------------------------
        // The AND. Every query term must match.
        // ------------------------------------------------------------------

        [Fact]
        public void A_game_matching_every_term_is_a_match()
        {
            Assert.True(Rank("Sonic the Hedgehog", "sonic hedge").IsMatch);
        }

        [Fact]
        public void A_game_missing_any_term_is_not_a_match()
        {
            Assert.False(Rank("Sonic the Hedgehog", "sonic mario").IsMatch);
        }

        /// <summary>
        /// VER-SEARCH-013. Terms narrow; they never widen. Adding a term the game does not carry
        /// takes it from matching to not matching.
        /// </summary>
        [Fact]
        public void Adding_a_term_narrows_rather_than_widens()
        {
            Assert.True(Rank("Sonic the Hedgehog", "sonic ").IsMatch);
            Assert.False(Rank("Sonic the Hedgehog", "sonic zelda").IsMatch);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void An_empty_query_matches_nothing(string buffer)
        {
            Assert.False(Rank("Sonic the Hedgehog", buffer).IsMatch);
        }

        [Fact]
        public void A_null_game_or_query_matches_nothing()
        {
            Assert.False(SearchScoring.Rank(null, SearchQuery.Parse("sonic")).IsMatch);
            Assert.False(SearchScoring.Rank(Game("Sonic"), null).IsMatch);
        }

        [Fact]
        public void Anything_that_matched_ranks_above_anything_that_did_not()
        {
            Assert.True(Beats(Rank("Sonic the Hedgehog", "sonic"),
                              Rank("Super Mario Bros", "sonic")));
        }

        // ------------------------------------------------------------------
        // Match quality - the signal that dominates.
        // ------------------------------------------------------------------

        /// <summary>
        /// A longer prefix of a term is a better match, so the score climbs with every keystroke
        /// the user commits.
        /// </summary>
        [Fact]
        public void A_longer_prefix_scores_higher_than_a_shorter_one()
        {
            double s = Rank("Sonic the Hedgehog", "s").TermScore;
            double so = Rank("Sonic the Hedgehog", "so").TermScore;
            double son = Rank("Sonic the Hedgehog", "son").TermScore;
            double sonic = Rank("Sonic the Hedgehog", "sonic").TermScore;

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

            Assert.Equal(Rank("Sonic the Hedgehog", "sonic ").TermScore,
                         Rank("Sonic the Hedgehog", "sonic").TermScore, 6);
        }

        /// <summary>
        /// THE STRUCTURAL GUARANTEE. A better match wins however unpopular, however long its
        /// title, wherever in the title it matched. Under the old additive score this held only
        /// because of the particular weights chosen; now nothing beneath match quality is ever
        /// consulted unless match quality ties.
        /// </summary>
        [Fact]
        public void A_better_match_beats_everything_stacked_against_it()
        {
            // "son" covers 3 of 5 characters of "sonic", but only 3 of 6 of "sonata"
            SearchRank betterMatch = Rank("Sonic Adventure Collection Special Edition", "son", 0);
            SearchRank worseMatch = Rank("Sonata", "son", 5);

            Assert.True(betterMatch.TermScore > worseMatch.TermScore);
            Assert.True(Beats(betterMatch, worseMatch));
        }

        // ------------------------------------------------------------------
        // The tiebreaks, in order.
        // ------------------------------------------------------------------

        /// <summary>
        /// The case that prompted the rework. Both titles contain the word exactly, so match
        /// quality ties and popularity decides - which is the only honest evidence available for
        /// which of two equally-matching games the user actually meant.
        /// </summary>
        [Fact]
        public void When_the_match_ties_the_more_popular_game_wins()
        {
            SearchRank famous = Rank("The Legend of Zelda", "zelda", 5);
            SearchRank sequel = Rank("Zelda II The Adventure of Link", "zelda", 3);

            Assert.Equal(famous.TermScore, sequel.TermScore, 6);
            Assert.True(Beats(famous, sequel));
        }

        /// <summary>
        /// And it still comes out right on a library with no ratings at all, because brevity is
        /// the next tiebreak and it points the same way.
        /// </summary>
        [Fact]
        public void With_no_ratings_at_all_the_shorter_title_wins()
        {
            SearchRank famous = Rank("The Legend of Zelda", "zelda", 0);
            SearchRank sequel = Rank("Zelda II The Adventure of Link", "zelda", 0);

            Assert.Equal(famous.Popularity, sequel.Popularity);
            Assert.True(Beats(famous, sequel));
        }

        /// <summary>The base game beats the collection that contains it.</summary>
        [Fact]
        public void A_short_title_beats_a_long_one_for_the_same_match()
        {
            Assert.True(Beats(Rank("Sonic the Hedgehog 2", "sonic"),
                              Rank("Sonic the Hedgehog 2 Special Edition Collection", "sonic")));
        }

        /// <summary>
        /// Popularity outranks brevity: a well-loved game with a longer title beats an unrated
        /// one with a short title, when both match equally well.
        /// </summary>
        [Fact]
        public void Popularity_is_consulted_before_brevity()
        {
            Assert.True(Beats(Rank("Sonic the Hedgehog Deluxe Edition", "sonic", 5),
                              Rank("Sonic", "sonic", 0)));
        }

        /// <summary>
        /// Matching the start of the title is the last word, not the first. Everything else has
        /// to tie before it is consulted - which is exactly what went wrong when it was worth a
        /// flat fifteen points in a sum.
        /// </summary>
        [Fact]
        public void Matching_the_start_of_the_title_breaks_a_complete_tie()
        {
            // same match quality, same popularity, same token count - only position differs
            SearchRank atStart = Rank("Hedgehog Launch Deluxe", "hedgehog", 3);
            SearchRank laterOn = Rank("Sonic the Hedgehog", "hedgehog", 3);

            Assert.Equal(atStart.TermScore, laterOn.TermScore, 6);
            Assert.Equal(atStart.Brevity, laterOn.Brevity, 6);
            Assert.True(atStart.MatchedFirstToken);
            Assert.False(laterOn.MatchedFirstToken);
            Assert.True(Beats(atStart, laterOn));
        }

        /// <summary>
        /// And it cannot do more than that. A game matching later in a better-known title beats
        /// one matching at the start of an obscure one.
        /// </summary>
        [Fact]
        public void Matching_the_start_cannot_outvote_popularity()
        {
            Assert.True(Beats(Rank("Sonic the Hedgehog", "hedgehog", 5),
                              Rank("Hedgehog Launch Deluxe", "hedgehog", 0)));
        }

        /// <summary>
        /// Brevity stops helping once a title is long enough - otherwise a very long title would
        /// keep being penalised for length it has already been penalised for.
        /// </summary>
        [Fact]
        public void The_brevity_score_bottoms_out_at_a_long_title()
        {
            Assert.Equal(Rank("Sonic a b c d e f g", "sonic").Brevity,
                         Rank("Sonic a b c d e f g h i j k", "sonic").Brevity, 6);
        }

        [Fact]
        public void Popularity_is_carried_through_from_the_game()
        {
            Assert.Equal(4, Rank("Sonic the Hedgehog", "sonic", 4).Popularity);
        }

        // ------------------------------------------------------------------
        // Numeral alternates have to survive into scoring, or a game found through
        // one would fail to match the term that found it.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("vii")]
        [InlineData("7")]
        [InlineData("seven")]
        public void A_game_found_through_a_numeral_alternate_still_matches(string spelling)
        {
            Assert.True(Rank("Final Fantasy VII", "final fantasy " + spelling).IsMatch);
        }

        [Fact]
        public void Every_spelling_of_a_number_ranks_the_same()
        {
            Assert.Equal(0, Rank("Final Fantasy VII", "final fantasy vii")
                                .CompareTo(Rank("Final Fantasy VII", "final fantasy 7")));
        }

        // ------------------------------------------------------------------
        // Multi-term behaviour.
        // ------------------------------------------------------------------

        /// <summary>
        /// Term scores are averaged rather than summed, so a two-term query stays on the same
        /// scale as a one-term query and a person reading a failing test can still tell roughly
        /// what happened.
        /// </summary>
        [Fact]
        public void Term_scores_are_averaged_so_more_terms_does_not_mean_a_bigger_number()
        {
            Assert.Equal(Rank("Sonic the Hedgehog", "sonic ").TermScore,
                         Rank("Sonic the Hedgehog", "sonic hedgehog ").TermScore, 6);
        }

        [Fact]
        public void Word_order_does_not_matter()
        {
            Assert.Equal(0, Rank("Sonic the Hedgehog", "sonic hedgehog ")
                                .CompareTo(Rank("Sonic the Hedgehog", "hedgehog sonic ")));
        }

        /// <summary>
        /// The first-token tiebreak is earned by whichever term matched the title's first word,
        /// which is what makes word order not matter for it either.
        /// </summary>
        [Fact]
        public void The_first_token_tiebreak_is_earned_whichever_term_matched_it()
        {
            Assert.True(Rank("Sonic the Hedgehog", "sonic hedgehog ").MatchedFirstToken);
            Assert.True(Rank("Sonic the Hedgehog", "hedgehog sonic ").MatchedFirstToken);
        }

        // ------------------------------------------------------------------
        // The comparison itself.
        // ------------------------------------------------------------------

        [Fact]
        public void Two_identical_ranks_compare_equal()
        {
            Assert.Equal(0, Rank("Sonic the Hedgehog", "sonic", 3)
                                .CompareTo(Rank("Sonic the Hedgehog", "sonic", 3)));
        }

        [Fact]
        public void Two_non_matches_compare_equal()
        {
            Assert.Equal(0, SearchRank.NoMatch.CompareTo(SearchRank.NoMatch));
            Assert.False(SearchRank.NoMatch.IsMatch);
        }
    }
}
