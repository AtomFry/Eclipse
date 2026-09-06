using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// Stage 4a - the facet index. Library data: which metadata values exist and which games
    /// carry each.
    ///
    /// Built entirely from object literals, with no catalog and no LaunchBox behind it, which is
    /// the point of GameFacets existing at all.
    /// </summary>
    public class FacetIndexTests
    {
        /// <summary>A game carrying the given values, at the given catalog position.</summary>
        private static GameFacets GameAt(int catalogIndex, params (ListCategoryType Facet, string Value)[] values)
        {
            return new GameFacets(catalogIndex,
                                  values.Select(v => new FacetValue(v.Facet, v.Value)).ToList());
        }

        // ------------------------------------------------------------------
        // Which facets exist and how they behave.
        // ------------------------------------------------------------------

        /// <summary>
        /// The algebra the whole filter feature rests on: OR within a facet, AND across facets -
        /// uniformly, for every facet (RULE-SEARCH-051).
        ///
        /// Every facet, deliberately. This was once a split, with platform and release year
        /// ORing because a game carries only one of each and everything else ANDing because the
        /// LaunchBox schema says a game can carry several. The schema was right and irrelevant:
        /// outside genre, real libraries do not use it, so ANDing two developers was empty and
        /// the second one was never even offered. See the note in FacetIndex for the measurement.
        ///
        /// Asserted through Apply rather than against a flag, so what is pinned is the behaviour
        /// rather than the switch that happens to produce it.
        /// </summary>
        [Theory]
        [InlineData(ListCategoryType.Platform)]
        [InlineData(ListCategoryType.ReleaseYear)]
        [InlineData(ListCategoryType.Genre)]
        [InlineData(ListCategoryType.Publisher)]
        [InlineData(ListCategoryType.Developer)]
        [InlineData(ListCategoryType.Series)]
        [InlineData(ListCategoryType.PlayMode)]
        public void Two_values_of_any_one_facet_mean_either(ListCategoryType facet)
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (facet, "one")),
                GameAt(1, (facet, "two")),
                GameAt(2, (facet, "three"))
            });

            int[] surviving = FilterSet.Apply(
                new[] { new SearchFilter(facet, "one"), new SearchFilter(facet, "two") },
                index);

            Assert.Equal(new[] { 0, 1 }, surviving);
        }

        /// <summary>
        /// The other half: different facets still narrow. Without this the uniform OR would have
        /// turned every filter into a widening one and left no way to narrow at all.
        /// </summary>
        [Fact]
        public void Two_values_of_different_facets_mean_both()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Shooter"), (ListCategoryType.Developer, "Capcom")),
                GameAt(1, (ListCategoryType.Genre, "Shooter"), (ListCategoryType.Developer, "Konami")),
                GameAt(2, (ListCategoryType.Genre, "Puzzle"), (ListCategoryType.Developer, "Capcom"))
            });

            int[] surviving = FilterSet.Apply(
                new[]
                {
                    new SearchFilter(ListCategoryType.Genre, "Shooter"),
                    new SearchFilter(ListCategoryType.Developer, "Capcom")
                },
                index);

            Assert.Equal(new[] { 0 }, surviving);
        }

        /// <summary>
        /// Facet order mirrors GameListBuilder.MoreLikeThisCategories, so search and "more like
        /// this" agree on what counts as the most relevant kind of metadata.
        /// </summary>
        [Fact]
        public void Series_and_genre_are_considered_before_the_rest()
        {
            Assert.Equal(ListCategoryType.Series, Facets.Filterable[0]);
            Assert.Equal(ListCategoryType.Genre, Facets.Filterable[1]);
        }

        // ------------------------------------------------------------------
        // Building.
        // ------------------------------------------------------------------

        [Fact]
        public void A_term_carries_every_game_that_has_its_value()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports")),
                GameAt(1, (ListCategoryType.Genre, "Sports")),
                GameAt(2, (ListCategoryType.Genre, "Puzzle"))
            });

            Assert.Equal(new[] { 0, 1 }, index.Games(ListCategoryType.Genre, "Sports"));
            Assert.Equal(new[] { 2 }, index.Games(ListCategoryType.Genre, "Puzzle"));
        }

        [Fact]
        public void A_game_can_carry_several_values_of_one_facet()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports"), (ListCategoryType.Genre, "Football"))
            });

            Assert.Equal(new[] { 0 }, index.Games(ListCategoryType.Genre, "Sports"));
            Assert.Equal(new[] { 0 }, index.Games(ListCategoryType.Genre, "Football"));
        }

        [Fact]
        public void The_same_value_in_two_facets_is_two_terms()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports")),
                GameAt(1, (ListCategoryType.Publisher, "Sports"))
            });

            Assert.Equal(new[] { 0 }, index.Games(ListCategoryType.Genre, "Sports"));
            Assert.Equal(new[] { 1 }, index.Games(ListCategoryType.Publisher, "Sports"));
            Assert.Equal(2, index.TermCount);
        }

        /// <summary>
        /// LaunchBox metadata is hand-entered, so the same platform arrives spelled several ways.
        /// They are one term, and the first spelling seen is the one shown.
        /// </summary>
        [Fact]
        public void Values_differing_only_in_case_are_one_term()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Platform, "Sega Genesis")),
                GameAt(1, (ListCategoryType.Platform, "sega genesis")),
                GameAt(2, (ListCategoryType.Platform, "SEGA GENESIS"))
            });

            Assert.Equal(1, index.TermCount);
            Assert.Equal(new[] { 0, 1, 2 }, index.Games(ListCategoryType.Platform, "SEGA GENESIS"));
            Assert.Equal("Sega Genesis", index.Terms[0].Value);
        }

        [Fact]
        public void Surrounding_whitespace_does_not_make_a_separate_term()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports")),
                GameAt(1, (ListCategoryType.Genre, "  Sports  "))
            });

            Assert.Equal(1, index.TermCount);
            Assert.Equal(new[] { 0, 1 }, index.Games(ListCategoryType.Genre, "Sports"));
        }

        [Fact]
        public void A_game_listing_the_same_value_twice_appears_in_its_postings_once()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports"), (ListCategoryType.Genre, "Sports"))
            });

            Assert.Equal(new[] { 0 }, index.Games(ListCategoryType.Genre, "Sports"));
        }

        [Fact]
        public void Postings_are_ascending_whatever_order_the_games_arrived_in()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(40, (ListCategoryType.Genre, "Sports")),
                GameAt(10, (ListCategoryType.Genre, "Sports")),
                GameAt(25, (ListCategoryType.Genre, "Sports"))
            });

            Assert.Equal(new[] { 10, 25, 40 }, index.Games(ListCategoryType.Genre, "Sports"));
        }

        /// <summary>
        /// The values are analysed the same way titles are, because a user types the distinctive
        /// word rather than the first one - "sports" has to reach "EA Sports".
        /// </summary>
        [Fact]
        public void A_terms_tokens_are_analysed_like_a_title()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Publisher, "EA Sports")),
                GameAt(1, (ListCategoryType.Platform, "Sega Genesis"))
            });

            Assert.Equal(new[] { "ea", "sports" }, index.Term(ListCategoryType.Publisher, "EA Sports").Tokens);
            Assert.Equal(new[] { "sega", "genesis" }, index.Term(ListCategoryType.Platform, "Sega Genesis").Tokens);
        }

        [Fact]
        public void A_term_knows_how_many_games_carry_it()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports")),
                GameAt(1, (ListCategoryType.Genre, "Sports"))
            });

            Assert.Equal(2, index.Term(ListCategoryType.Genre, "Sports").GameCount);
        }

        // ------------------------------------------------------------------
        // Nothing to index, or nothing worth indexing.
        // ------------------------------------------------------------------

        [Fact]
        public void An_empty_library_builds_an_empty_index()
        {
            FacetIndex index = FacetIndex.Build(new GameFacets[0]);

            Assert.Equal(0, index.TermCount);
            Assert.Empty(index.Games(ListCategoryType.Genre, "Sports"));
            Assert.Null(index.Term(ListCategoryType.Genre, "Sports"));
        }

        [Fact]
        public void A_null_library_builds_an_empty_index()
        {
            Assert.Equal(0, FacetIndex.Build(null).TermCount);
        }

        [Fact]
        public void The_empty_index_answers_without_throwing()
        {
            Assert.Empty(FacetIndex.Empty.Terms);
            Assert.Empty(FacetIndex.Empty.Games(ListCategoryType.Genre, "Sports"));
            Assert.Null(FacetIndex.Empty.Term(ListCategoryType.Genre, "Sports"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_blank_metadata_value_is_not_a_term(string value)
        {
            FacetIndex index = FacetIndex.Build(new[] { GameAt(0, (ListCategoryType.Genre, value)) });

            Assert.Equal(0, index.TermCount);
        }

        [Fact]
        public void A_null_game_in_the_library_is_skipped()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports")),
                null
            });

            Assert.Equal(new[] { 0 }, index.Games(ListCategoryType.Genre, "Sports"));
        }

        /// <summary>
        /// Playlists are a relationship held outside the game, so the projection does not supply
        /// them and the index does not carry them. Filtering by one is not offered rather than
        /// being silently empty.
        /// </summary>
        [Fact]
        public void A_facet_search_does_not_filter_on_is_not_indexed()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Playlist, "My Favourites"))
            });

            Assert.Equal(0, index.TermCount);
            Assert.Empty(index.Games(ListCategoryType.Playlist, "My Favourites"));
        }

        // ------------------------------------------------------------------
        // Enumerating.
        // ------------------------------------------------------------------

        [Fact]
        public void Terms_are_grouped_by_facet_in_the_documented_order()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0,
                    (ListCategoryType.Developer, "Sonic Team"),
                    (ListCategoryType.Genre, "Platform"),
                    (ListCategoryType.Series, "Sonic"))
            });

            Assert.Equal(new[] { ListCategoryType.Series, ListCategoryType.Genre, ListCategoryType.Developer },
                         index.Terms.Select(term => term.Facet));
        }

        [Fact]
        public void Terms_within_a_facet_are_value_ordered()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports")),
                GameAt(1, (ListCategoryType.Genre, "Action")),
                GameAt(2, (ListCategoryType.Genre, "Puzzle"))
            });

            Assert.Equal(new[] { "Action", "Puzzle", "Sports" },
                         index.Terms.Select(term => term.Value));
        }

        [Fact]
        public void The_terms_of_one_facet_can_be_listed()
        {
            FacetIndex index = FacetIndex.Build(new[]
            {
                GameAt(0, (ListCategoryType.Genre, "Sports"), (ListCategoryType.Platform, "Sega Genesis")),
                GameAt(1, (ListCategoryType.Genre, "Football"))
            });

            IReadOnlyList<FacetTerm> genres = index.TermsIn(ListCategoryType.Genre);

            Assert.Equal(new[] { "Football", "Sports" }, genres.Select(term => term.Value));
            Assert.Empty(index.TermsIn(ListCategoryType.Developer));
        }
    }
}
