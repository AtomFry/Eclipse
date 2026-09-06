using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// Stage 4c - which metadata terms are offered, in what order, and with what counts.
    /// VER-SEARCH-023, 024.
    ///
    /// The invariant most of these exist to protect: counts are computed against the current
    /// filter set and a term that would produce nothing is never offered, which together make it
    /// impossible to reach an empty result set by picking suggestions.
    /// </summary>
    public class SuggestionRankerTests
    {
        private const int MaxSuggestions = 8;
        private const int MaxPerFacet = 3;

        private static GameFacets GameAt(int catalogIndex, params (ListCategoryType Facet, string Value)[] values)
        {
            return new GameFacets(catalogIndex, values.Select(v => new FacetValue(v.Facet, v.Value)).ToList());
        }

        /// <summary>
        ///   0  Sports, Football   Sega Genesis   EA Sports
        ///   1  Sports             Sega Genesis   EA Sports
        ///   2  Sports, Football   SNES           Nintendo
        ///   3  Puzzle             Sega Genesis   Sega
        /// </summary>
        private static FacetIndex Library()
        {
            return FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports"), (ListCategoryType.Genre, "Football"),
                          (ListCategoryType.Platform, "Sega Genesis"), (ListCategoryType.Publisher, "EA Sports")),
                GameAt(1, (ListCategoryType.Genre, "Sports"),
                          (ListCategoryType.Platform, "Sega Genesis"), (ListCategoryType.Publisher, "EA Sports")),
                GameAt(2, (ListCategoryType.Genre, "Sports"), (ListCategoryType.Genre, "Football"),
                          (ListCategoryType.Platform, "SNES"), (ListCategoryType.Publisher, "Nintendo")),
                GameAt(3, (ListCategoryType.Genre, "Puzzle"),
                          (ListCategoryType.Platform, "Sega Genesis"), (ListCategoryType.Publisher, "Sega"))
            });
        }

        private static IReadOnlyList<Suggestion> Rank(string query,
                                                     IReadOnlyList<SearchFilter> applied = null,
                                                     int[] filterSet = null,
                                                     FacetIndex index = null)
        {
            return SuggestionRanker.Rank(query, index ?? Library(), applied, filterSet,
                                         MaxSuggestions, MaxPerFacet);
        }

        private static SearchFilter Genre(string value) => new SearchFilter(ListCategoryType.Genre, value);

        // ------------------------------------------------------------------
        // Matching.
        // ------------------------------------------------------------------

        [Fact]
        public void A_prefix_of_a_value_is_offered()
        {
            Assert.Contains(Rank("spo"), s => s.Value == "Sports");
        }

        /// <summary>
        /// Users type the distinctive word, not the first one. "sports" has to reach "EA Sports"
        /// and "genesis" has to reach "Sega Genesis".
        /// </summary>
        [Fact]
        public void A_prefix_of_any_word_in_a_value_is_offered()
        {
            Assert.Contains(Rank("genesis"), s => s.Value == "Sega Genesis");
            Assert.Contains(Rank("sports"), s => s.Value == "EA Sports");
        }

        [Fact]
        public void A_multi_word_query_matches_the_whole_value()
        {
            IReadOnlyList<Suggestion> found = Rank("ea sports");

            Assert.Single(found);
            Assert.Equal("EA Sports", found[0].Value);
            Assert.Equal(SuggestionMatch.ExactValue, found[0].Match);
        }

        [Fact]
        public void A_query_matching_nothing_offers_nothing()
        {
            Assert.Empty(Rank("zzz"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void An_empty_query_offers_nothing(string query)
        {
            Assert.Empty(Rank(query));
        }

        [Fact]
        public void A_null_index_offers_nothing()
        {
            Assert.Empty(SuggestionRanker.Rank("spo", null, null, null, MaxSuggestions, MaxPerFacet));
        }

        /// <summary>
        /// RULE-SEARCH-059's threshold is a display decision made by the session. The ranker
        /// itself answers a one-character query perfectly well, so where the line goes stays a
        /// question for a real library rather than being baked in here.
        /// </summary>
        [Fact]
        public void The_ranker_answers_a_single_character_query()
        {
            Assert.NotEmpty(Rank("s"));
        }

        // ------------------------------------------------------------------
        // Ranking - RULE-SEARCH-057.
        // ------------------------------------------------------------------

        [Fact]
        public void An_exact_value_outranks_a_prefix_of_a_longer_one()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sport")),
                GameAt(1, (ListCategoryType.Genre, "Sports")),
                GameAt(2, (ListCategoryType.Genre, "Sports")),
                GameAt(3, (ListCategoryType.Genre, "Sports"))
            });

            IReadOnlyList<Suggestion> found = Rank("sport", index: index);

            // "Sport" is exact with one game; "Sports" is a prefix with three. Match quality
            // decides first, so the count cannot lift the weaker match above it.
            Assert.Equal("Sport", found[0].Value);
            Assert.Equal(SuggestionMatch.ExactValue, found[0].Match);
        }

        /// <summary>
        /// "sports" is the whole of the genre value and only a word of the publisher's, so the
        /// genre is an exact match and the publisher a word match - and the exact one leads.
        /// </summary>
        [Fact]
        public void A_whole_value_match_outranks_a_word_prefix()
        {
            IReadOnlyList<Suggestion> found = Rank("sports");

            Assert.Equal("Sports", found[0].Value);
            Assert.Equal(SuggestionMatch.ExactValue, found[0].Match);
            Assert.Contains(found, s => s.Value == "EA Sports" && s.Match == SuggestionMatch.TokenPrefix);
        }

        [Fact]
        public void A_prefix_of_the_whole_value_outranks_a_prefix_of_a_later_word()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Publisher, "EA Sports")),
                GameAt(1, (ListCategoryType.Genre, "Beat em up EA"))
            });

            IReadOnlyList<Suggestion> found = Rank("ea", index: index);

            Assert.Equal("EA Sports", found[0].Value);
            Assert.Equal(SuggestionMatch.ValuePrefix, found[0].Match);
            Assert.Equal(SuggestionMatch.TokenPrefix, found[1].Match);
        }

        [Fact]
        public void Equal_matches_are_ordered_by_how_many_games_they_would_leave()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports")),
                GameAt(1, (ListCategoryType.Genre, "Sports")),
                GameAt(2, (ListCategoryType.Genre, "Spooky"))
            });

            IReadOnlyList<Suggestion> found = Rank("sp", index: index);

            Assert.Equal("Sports", found[0].Value);
            Assert.Equal(2, found[0].Count);
            Assert.Equal("Spooky", found[1].Value);
        }

        [Fact]
        public void The_order_does_not_change_between_identical_calls()
        {
            Assert.Equal(Rank("s").Select(s => s.Value), Rank("s").Select(s => s.Value));
        }

        // ------------------------------------------------------------------
        // Counts - RULE-SEARCH-054, 056. VER-SEARCH-023.
        // ------------------------------------------------------------------

        [Fact]
        public void With_nothing_filtered_a_count_is_the_whole_library()
        {
            Assert.Equal(3, Rank("sports").Single(s => s.Value == "Sports").Count);
        }

        /// <summary>
        /// The invariant. Counts are against the games the applied filters leave, so what a
        /// suggestion promises is what selecting it delivers.
        /// </summary>
        [Fact]
        public void A_count_is_computed_against_the_current_filter_set()
        {
            FacetIndex index = Library();
            int[] genesis = FilterSet.Apply(
                new[] { new SearchFilter(ListCategoryType.Platform, "Sega Genesis") }, index);

            // Sports has three games library-wide, but only two on the Genesis.
            Assert.Equal(3, Rank("sports", index: index).Single(s => s.Value == "Sports").Count);
            Assert.Equal(2, Rank("sports", filterSet: genesis, index: index).Single(s => s.Value == "Sports").Count);
        }

        /// <summary>
        /// RULE-SEARCH-055. The user cannot select Football after filtering to the Genesis unless
        /// a Genesis football game exists - it simply is not offered, so the screen never has to
        /// explain a dead end because it never creates one.
        /// </summary>
        [Fact]
        public void A_term_that_would_produce_nothing_is_never_offered()
        {
            FacetIndex index = Library();
            int[] puzzle = FilterSet.Apply(new[] { Genre("Puzzle") }, index);

            Assert.Contains(Rank("foot", index: index), s => s.Value == "Football");
            Assert.DoesNotContain(Rank("foot", filterSet: puzzle, index: index), s => s.Value == "Football");
        }

        [Fact]
        public void Nothing_offered_ever_has_a_count_of_zero()
        {
            FacetIndex index = Library();
            int[] snes = FilterSet.Apply(
                new[] { new SearchFilter(ListCategoryType.Platform, "SNES") }, index);

            Assert.All(Rank("s", filterSet: snes, index: index), s => Assert.True(s.Count > 0));
        }

        /// <summary>
        /// The count deliberately ignores what is currently typed, because selecting a suggestion
        /// clears the query - a count that included the text would promise a number that
        /// selecting it does not deliver.
        /// </summary>
        [Fact]
        public void A_count_does_not_account_for_the_text_that_produced_it()
        {
            // "spo" matches only Sports, but the count is every sports game, not every sports
            // game whose title also matches "spo".
            Assert.Equal(3, Rank("spo").Single(s => s.Value == "Sports").Count);
        }

        // ------------------------------------------------------------------
        // Already applied - RULE-SEARCH-061.
        // ------------------------------------------------------------------

        [Fact]
        public void An_applied_term_is_still_offered_and_marked()
        {
            FacetIndex index = Library();
            SearchFilter[] applied = { Genre("Sports") };
            int[] filterSet = FilterSet.Apply(applied, index);

            Suggestion sports = Rank("sports", applied, filterSet, index).Single(s => s.Value == "Sports");

            Assert.True(sports.IsApplied);
        }

        [Fact]
        public void A_term_that_is_not_applied_is_not_marked()
        {
            Assert.All(Rank("sports"), s => Assert.False(s.IsApplied));
        }

        /// <summary>
        /// An applied term survives the zero-count rule, so the user can always see what they
        /// have on and take it off again.
        /// </summary>
        [Fact]
        public void An_applied_term_is_kept_even_where_it_would_be_dropped()
        {
            FacetIndex index = Library();
            SearchFilter[] applied = { Genre("Puzzle") };

            // A filter set that excludes everything - the applied term still shows.
            Assert.Contains(Rank("puzzle", applied, new int[0], index), s => s.Value == "Puzzle" && s.IsApplied);
        }

        [Fact]
        public void A_suggestion_knows_the_filter_it_would_apply()
        {
            Suggestion sports = Rank("sports").Single(s => s.Value == "Sports");

            Assert.Equal(new SearchFilter(ListCategoryType.Genre, "Sports"), sports.AsFilter());
        }

        // ------------------------------------------------------------------
        // Capping - RULE-SEARCH-058. VER-SEARCH-024.
        // ------------------------------------------------------------------

        [Fact]
        public void No_more_than_the_maximum_are_offered()
        {
            List<GameFacets> games = new List<GameFacets>();
            for (int index = 0; index < 40; index++)
            {
                games.Add(GameAt(index,
                    (ListCategoryType.Developer, "Sp Dev " + index),
                    (ListCategoryType.Publisher, "Sp Pub " + index),
                    (ListCategoryType.Genre, "Sp Genre " + index)));
            }

            Assert.Equal(MaxSuggestions,
                         SuggestionRanker.Rank("sp", FacetIndex.Build(games), null, null,
                                               MaxSuggestions, MaxPerFacet).Count);
        }

        /// <summary>
        /// VER-SEARCH-024, and the half of the capping rule that matters.
        ///
        /// Ten developers with five games each, against one genre with a single game. Ranked on
        /// count alone every developer outranks the genre and it would fall off the end of the
        /// list entirely. The per-facet cap is what gets it in.
        /// </summary>
        [Fact]
        public void One_facet_cannot_crowd_out_the_others()
        {
            List<GameFacets> games = new List<GameFacets> { GameAt(0, (ListCategoryType.Genre, "Sports")) };

            int catalogIndex = 1;
            for (int studio = 0; studio < 10; studio++)
            {
                for (int game = 0; game < 5; game++)
                {
                    games.Add(GameAt(catalogIndex++, (ListCategoryType.Developer, "Sp Studio " + studio)));
                }
            }

            IReadOnlyList<Suggestion> found =
                SuggestionRanker.Rank("sp", FacetIndex.Build(games), null, null, MaxSuggestions, MaxPerFacet);

            Assert.Contains(found, s => s.Value == "Sports");
            Assert.All(found.Where(s => s.Facet == ListCategoryType.Developer), s => Assert.Equal(5, s.Count));
        }

        /// <summary>
        /// Diversity is a floor, not a ceiling. Where every candidate belongs to one facet there
        /// is nothing to crowd out, so holding the list to the per-facet cap would hide useful
        /// terms to protect a diversity that cannot exist.
        /// </summary>
        [Fact]
        public void The_per_facet_cap_does_not_waste_slots_when_there_is_only_one_facet()
        {
            List<GameFacets> games = new List<GameFacets>();
            for (int index = 0; index < 40; index++)
            {
                games.Add(GameAt(index, (ListCategoryType.Developer, "Studio " + index)));
            }

            IReadOnlyList<Suggestion> found =
                SuggestionRanker.Rank("studio", FacetIndex.Build(games), null, null, MaxSuggestions, MaxPerFacet);

            Assert.Equal(MaxSuggestions, found.Count);
            Assert.Equal(MaxSuggestions, found.Select(s => s.Value).Distinct().Count());
        }

        [Fact]
        public void A_maximum_of_zero_offers_nothing()
        {
            Assert.Empty(SuggestionRanker.Rank("spo", Library(), null, null, 0, MaxPerFacet));
        }
    }
}
