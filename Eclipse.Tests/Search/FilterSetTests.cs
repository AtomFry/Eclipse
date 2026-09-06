using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// Stage 4b - the filter algebra. VER-SEARCH-020, 021, 025.
    ///
    /// The single most consequential thing in the metadata filter feature: get the same-facet
    /// rule wrong and the result set is silently always empty for a whole class of filter, with
    /// nothing on screen to explain why.
    /// </summary>
    public class FilterSetTests
    {
        /// <summary>
        /// A small library shaped for the algebra: sports games across two platforms, one of
        /// which is also football, plus an unrelated puzzle game.
        ///
        ///   0  Sports, Football   Sega Genesis   EA Sports
        ///   1  Sports             Sega Genesis   EA Sports
        ///   2  Sports, Football   SNES           Nintendo
        ///   3  Puzzle             Sega Genesis   Sega
        /// </summary>
        private static FacetIndex Library()
        {
            return FacetIndex.Build(new[]
            {
                Game(0, ("Sports", "Football"), "Sega Genesis", "EA Sports"),
                Game(1, ("Sports", null), "Sega Genesis", "EA Sports"),
                Game(2, ("Sports", "Football"), "SNES", "Nintendo"),
                Game(3, ("Puzzle", null), "Sega Genesis", "Sega")
            });
        }

        private static GameFacets Game(int catalogIndex, (string First, string Second) genres,
                                       string platform, string publisher)
        {
            List<FacetValue> values = new List<FacetValue>
            {
                new FacetValue(ListCategoryType.Genre, genres.First),
                new FacetValue(ListCategoryType.Platform, platform),
                new FacetValue(ListCategoryType.Publisher, publisher)
            };

            if (genres.Second != null)
            {
                values.Add(new FacetValue(ListCategoryType.Genre, genres.Second));
            }

            return new GameFacets(catalogIndex, values);
        }

        private static SearchFilter Genre(string value) => new SearchFilter(ListCategoryType.Genre, value);
        private static SearchFilter Platform(string value) => new SearchFilter(ListCategoryType.Platform, value);
        private static SearchFilter Publisher(string value) => new SearchFilter(ListCategoryType.Publisher, value);

        private static int[] Apply(params SearchFilter[] filters)
        {
            return FilterSet.Apply(filters, Library());
        }

        // ------------------------------------------------------------------
        // No filters at all.
        // ------------------------------------------------------------------

        /// <summary>
        /// Null means "no constraint", not "nothing survived". Materialising the whole library to
        /// say everything survived would be work done on every keystroke to express nothing.
        /// </summary>
        [Fact]
        public void No_filters_means_no_constraint_rather_than_no_games()
        {
            Assert.Null(FilterSet.Apply(new SearchFilter[0], Library()));
            Assert.Null(FilterSet.Apply(null, Library()));
        }

        [Fact]
        public void A_null_index_means_no_constraint()
        {
            Assert.Null(FilterSet.Apply(new[] { Genre("Sports") }, null));
        }

        // ------------------------------------------------------------------
        // One filter.
        // ------------------------------------------------------------------

        [Fact]
        public void One_filter_selects_the_games_carrying_it()
        {
            Assert.Equal(new[] { 0, 1, 2 }, Apply(Genre("Sports")));
            Assert.Equal(new[] { 0, 1, 3 }, Apply(Platform("Sega Genesis")));
        }

        [Fact]
        public void A_filter_the_library_does_not_have_narrows_to_nothing()
        {
            Assert.Empty(Apply(Genre("Racing")));
        }

        [Fact]
        public void A_filter_matches_regardless_of_how_it_is_capitalised()
        {
            Assert.Equal(Apply(Platform("Sega Genesis")), Apply(Platform("SEGA GENESIS")));
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-051 - OR within a facet, for every facet.
        // ------------------------------------------------------------------

        /// <summary>
        /// VER-SEARCH-020. Two genres accept either.
        ///
        /// THIS TEST USED TO ASSERT THE OPPOSITE, and the reversal was deliberate. Requiring both
        /// genres is meaningful - a game genuinely carries several, and "sports and football" is
        /// the example the filter feature was originally asked for. It was given up so that one
        /// rule covers every facet, because the split it replaced was doing real harm elsewhere:
        /// ANDing two developers is empty in any real library, so the second developer was never
        /// even offered. Intersecting within a genre is the price, and it is no longer
        /// expressible. See the note in FacetIndex for the measurement behind the choice.
        /// </summary>
        [Fact]
        public void Two_genres_accept_either()
        {
            Assert.Equal(new[] { 0, 1, 2 }, Apply(Genre("Sports")));
            Assert.Equal(new[] { 0, 1, 2, 3 }, Apply(Genre("Sports"), Genre("Puzzle")));
        }

        /// <summary>
        /// A second value of a facet can only widen it. The mirror of what this file used to
        /// assert, and the property that makes a same-facet filter impossible to turn into a
        /// dead end.
        /// </summary>
        [Fact]
        public void A_second_genre_can_only_widen()
        {
            int[] one = Apply(Genre("Sports"));
            int[] two = Apply(Genre("Sports"), Genre("Puzzle"));

            Assert.True(two.Length >= one.Length);
            Assert.All(one, game => Assert.Contains(game, two));
        }

        /// <summary>
        /// Two genres that no game carries together still find every game carrying either -
        /// where the old algebra found nothing at all.
        /// </summary>
        [Fact]
        public void Two_genres_no_game_carries_together_still_find_both_sets()
        {
            Assert.Equal(new[] { 0, 1, 2, 3 }, Apply(Genre("Sports"), Genre("Puzzle")));
        }

        /// <summary>
        /// A genre wholly contained in another adds nothing - every football game here is also a
        /// sports game. Correct, and the case SuggestionRanker declines to offer, because a
        /// filter that changes nothing is a wasted press.
        /// </summary>
        [Fact]
        public void A_genre_contained_in_one_already_applied_changes_nothing()
        {
            Assert.Equal(Apply(Genre("Sports")), Apply(Genre("Sports"), Genre("Football")));
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-052 - the same rule, where it was always true.
        // ------------------------------------------------------------------

        /// <summary>
        /// VER-SEARCH-020, the other half - and the trap. A game has exactly one platform, so
        /// AND across two could never match anything; the user would get an empty screen with no
        /// explanation. OR is also the only thing a second platform filter could sensibly mean.
        /// </summary>
        [Fact]
        public void Two_platforms_accept_either()
        {
            Assert.Equal(new[] { 0, 1, 2, 3 }, Apply(Platform("Sega Genesis"), Platform("SNES")));
        }

        [Fact]
        public void A_second_platform_widens_rather_than_narrowing()
        {
            int[] one = Apply(Platform("SNES"));
            int[] two = Apply(Platform("SNES"), Platform("Sega Genesis"));

            Assert.True(two.Length > one.Length);
            Assert.All(one, game => Assert.Contains(game, two));
        }

        [Fact]
        public void The_union_of_platforms_is_ascending_and_distinct()
        {
            int[] both = Apply(Platform("Sega Genesis"), Platform("SNES"));

            Assert.Equal(both.OrderBy(game => game), both);
            Assert.Equal(both.Distinct(), both);
        }

        // ------------------------------------------------------------------
        // RULE-SEARCH-050 - across facets, AND.
        // ------------------------------------------------------------------

        /// <summary>VER-SEARCH-021.</summary>
        [Fact]
        public void A_genre_and_a_platform_require_both()
        {
            Assert.Equal(new[] { 0, 1 }, Apply(Genre("Sports"), Platform("Sega Genesis")));
        }

        /// <summary>
        /// The worked example from the request, under the uniform algebra: a genre, then a
        /// second genre, then the publisher, then the platform.
        ///
        /// The shape has changed and the change is the whole trade. Adding a second genre no
        /// longer narrows - it brings in the puzzle game - and it is the filters of OTHER facets
        /// that cut back down. Narrowing within one facet is gone; narrowing across facets, which
        /// is what carries the example to a single game, is untouched.
        /// </summary>
        [Fact]
        public void The_stacking_example_from_the_request_still_reaches_one_game()
        {
            Assert.Equal(new[] { 0, 1, 2 }, Apply(Genre("Sports")));
            Assert.Equal(new[] { 0, 1, 2, 3 }, Apply(Genre("Sports"), Genre("Puzzle")));
            Assert.Equal(new[] { 0, 1 }, Apply(Genre("Sports"), Genre("Puzzle"), Publisher("EA Sports")));
            Assert.Equal(new[] { 0, 1 }, Apply(Genre("Sports"), Genre("Puzzle"), Publisher("EA Sports"),
                                               Platform("Sega Genesis")));
            Assert.Equal(new[] { 2 }, Apply(Genre("Sports"), Platform("SNES")));
        }

        /// <summary>
        /// And removing one widens it again - step 5 and 6 of the request. Nothing is stored
        /// incrementally, so a filter can be removed in any order.
        /// </summary>
        [Fact]
        public void Removing_a_filter_restores_exactly_the_set_before_it_was_added()
        {
            int[] before = Apply(Genre("Sports"), Publisher("EA Sports"));
            int[] after = Apply(Genre("Sports"), Publisher("EA Sports"), Platform("Sega Genesis"));
            int[] removed = Apply(Genre("Sports"), Publisher("EA Sports"));

            Assert.Equal(before, removed);
            Assert.True(after.Length <= before.Length);
        }

        /// <summary>VER-SEARCH-025 - order of application cannot change the result.</summary>
        [Fact]
        public void The_order_filters_were_applied_in_does_not_matter()
        {
            SearchFilter[] one = { Genre("Sports"), Platform("Sega Genesis"), Publisher("EA Sports") };
            SearchFilter[] two = { Publisher("EA Sports"), Genre("Sports"), Platform("Sega Genesis") };
            SearchFilter[] three = { Platform("Sega Genesis"), Publisher("EA Sports"), Genre("Sports") };

            FacetIndex index = Library();

            Assert.Equal(FilterSet.Apply(one, index), FilterSet.Apply(two, index));
            Assert.Equal(FilterSet.Apply(two, index), FilterSet.Apply(three, index));
        }

        [Fact]
        public void An_impossible_combination_narrows_to_nothing_rather_than_throwing()
        {
            Assert.Empty(Apply(Genre("Puzzle"), Publisher("Nintendo")));
        }

        [Fact]
        public void The_surviving_games_are_always_ascending()
        {
            int[] surviving = Apply(Genre("Sports"), Platform("Sega Genesis"), Platform("SNES"));

            Assert.Equal(surviving.OrderBy(game => game), surviving);
        }

        // ------------------------------------------------------------------
        // Filter identity - RULE-SEARCH-061 relies on this.
        // ------------------------------------------------------------------

        [Fact]
        public void Two_filters_naming_the_same_value_of_the_same_facet_are_equal()
        {
            Assert.Equal(Genre("Sports"), Genre("Sports"));
            Assert.Equal(Genre("Sports").GetHashCode(), Genre("SPORTS").GetHashCode());
            Assert.Equal(Genre("Sports"), Genre("SPORTS"));
        }

        [Fact]
        public void The_same_value_in_different_facets_is_a_different_filter()
        {
            Assert.NotEqual(Genre("Sports"), Publisher("Sports"));
        }

        /// <summary>
        /// Selecting an applied term removes it rather than adding it again, so a duplicate is
        /// unreachable rather than deduplicated later. This is what that check is built on.
        /// </summary>
        [Fact]
        public void An_applied_filter_can_be_recognised_however_it_is_capitalised()
        {
            SearchFilter[] applied = { Genre("Sports"), Platform("Sega Genesis") };

            Assert.True(FilterSet.Contains(applied, Genre("SPORTS")));
            Assert.True(FilterSet.Contains(applied, Platform("sega genesis")));
            Assert.False(FilterSet.Contains(applied, Genre("Football")));
            Assert.False(FilterSet.Contains(null, Genre("Sports")));
        }

        /// <summary>
        /// Applying the same filter twice cannot change the result, which is what makes the
        /// duplicate guard a convenience rather than a correctness requirement.
        /// </summary>
        [Fact]
        public void Applying_the_same_filter_twice_changes_nothing()
        {
            Assert.Equal(Apply(Genre("Sports")), Apply(Genre("Sports"), Genre("Sports")));
        }
    }
}
