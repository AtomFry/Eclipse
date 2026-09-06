using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-031 - the facet half of the golden corpus.
    ///
    /// The unit tests each pin one component: FacetIndex builds, FilterSet combines,
    /// SuggestionRanker ranks. This is the test that catches what none of them can - the three
    /// running together over data shaped like a real library, where a suggestion's promised count
    /// has to survive being applied and the algebra has to hold across facets that were never
    /// designed together.
    ///
    /// Assertions here are about which terms are offered and which games survive, never about
    /// internal numbers. Where an ordering is asserted it is because the ordering is the rule.
    /// </summary>
    public class SearchCorpusFacetTests
    {
        private static IReadOnlyList<string> Values(IEnumerable<Suggestion> suggestions)
        {
            return suggestions.Select(suggestion => suggestion.Value).ToList();
        }

        // ------------------------------------------------------------------
        // The requested script, end to end over realistic metadata.
        // ------------------------------------------------------------------

        /// <summary>
        /// The request's script, as it runs under the uniform algebra: type "spo", pick Sports,
        /// then narrow with a facet of a different kind.
        ///
        /// The original script picked Sports then Football and expected only games carrying both.
        /// That is no longer expressible - a second genre widens now (RULE-SEARCH-051) - and the
        /// trade was made knowingly. What the script was really demonstrating, stacking filters
        /// to reach a small set from a typed word, still works; it is the second constraint that
        /// has to come from a different facet to do the narrowing.
        /// </summary>
        [Fact]
        public void The_requested_script_still_reaches_a_small_set()
        {
            Assert.Contains("Sports", Values(SearchCorpus.Suggest("spo")));

            Assert.Equal(new[]
            {
                "FIFA International Soccer",
                "John Madden Football",
                "NBA Jam",
                "NHL Hockey",
                "Tecmo Super Bowl"
            }, SearchCorpus.Filtered(SearchCorpus.Genre("Sports")));

            // And Football is no longer offered at all here. Every football game in the corpus is
            // also tagged Sports, so under a uniform OR adding it would bring in nothing and
            // leave the screen exactly as it was - which SuggestionRanker declines to offer. The
            // old algebra offered it because it narrowed. A user who wants football alone takes
            // the Sports chip off first; there is no single press that swaps one for the other.
            Assert.DoesNotContain("Football", Values(SearchCorpus.Suggest("fo", SearchCorpus.Genre("Sports"))));

            Assert.Equal(new[] { "FIFA International Soccer", "John Madden Football", "NHL Hockey" },
                         SearchCorpus.Filtered(SearchCorpus.Genre("Sports"),
                                               SearchCorpus.Publisher("EA Sports")));
        }

        /// <summary>
        /// Step 3: the publisher narrows it further. "EA Sports" is also the case the plan asked
        /// the corpus to carry - a publisher whose name contains a word that is also a genre.
        /// </summary>
        [Fact]
        public void A_publisher_whose_name_contains_a_genre_word_is_a_separate_filter()
        {
            IReadOnlyList<Suggestion> offered = SearchCorpus.Suggest("sports");

            Assert.Contains(offered, s => s.Value == "Sports" && s.Facet == ListCategoryType.Genre);
            Assert.Contains(offered, s => s.Value == "EA Sports" && s.Facet == ListCategoryType.Publisher);

            // They select genuinely different sets - the publisher's football game, not both.
            Assert.Equal(new[] { "John Madden Football" },
                         SearchCorpus.Filtered(SearchCorpus.Genre("Football"), SearchCorpus.Publisher("EA Sports")));
        }

        /// <summary>
        /// Steps 5 and 6: removing a filter widens again, and the set that survives is always
        /// exactly what the remaining filters say. Nothing is stored incrementally.
        /// </summary>
        [Fact]
        public void Removing_a_filter_restores_the_set_that_preceded_it()
        {
            IReadOnlyList<string> sportsAndFootball =
                SearchCorpus.Filtered(SearchCorpus.Genre("Sports"), SearchCorpus.Genre("Football"));

            IReadOnlyList<string> narrowed = SearchCorpus.Filtered(
                SearchCorpus.Genre("Sports"), SearchCorpus.Genre("Football"), SearchCorpus.Publisher("EA Sports"));

            Assert.True(narrowed.Count < sportsAndFootball.Count);

            Assert.Equal(sportsAndFootball,
                         SearchCorpus.Filtered(SearchCorpus.Genre("Sports"), SearchCorpus.Genre("Football")));
        }

        // ------------------------------------------------------------------
        // The invariant: what a suggestion promises is what selecting it delivers.
        // ------------------------------------------------------------------

        /// <summary>
        /// RULE-SEARCH-056 across the whole corpus. Every suggestion offered for a spread of
        /// queries, under a spread of filters, has to produce exactly the number of games it
        /// said it would.
        /// </summary>
        [Fact]
        public void Every_suggestion_delivers_the_count_it_promised()
        {
            SearchFilter[][] contexts =
            {
                new SearchFilter[0],
                new[] { SearchCorpus.Genre("Sports") },
                new[] { SearchCorpus.Platform("Sega Genesis") },
                new[] { SearchCorpus.Genre("RPG"), SearchCorpus.Publisher("Square") },
                new[] { SearchCorpus.Platform("Nintendo Entertainment System"), SearchCorpus.Platform("Super Nintendo") }
            };

            foreach (SearchFilter[] applied in contexts)
            {
                foreach (string query in new[] { "s", "so", "spo", "ea", "nin", "sega", "rp", "act", "con" })
                {
                    foreach (Suggestion suggestion in SearchCorpus.Suggest(query, applied))
                    {
                        if (suggestion.IsApplied)
                        {
                            continue;
                        }

                        SearchFilter[] withIt = applied.Concat(new[] { suggestion.AsFilter() }).ToArray();

                        Assert.Equal(suggestion.Count, SearchCorpus.Filtered(withIt).Count);
                    }
                }
            }
        }

        /// <summary>
        /// RULE-SEARCH-055 across the whole corpus. Selecting any offered suggestion, from any
        /// context, must never produce an empty screen - which is what makes the filter feature
        /// safe to explore rather than a gamble.
        /// </summary>
        [Fact]
        public void No_offered_suggestion_can_lead_to_an_empty_result()
        {
            SearchFilter[][] contexts =
            {
                new SearchFilter[0],
                new[] { SearchCorpus.Genre("Sports") },
                new[] { SearchCorpus.Genre("Football") },
                new[] { SearchCorpus.Platform("Game Boy") },
                new[] { SearchCorpus.Publisher("Konami") }
            };

            foreach (SearchFilter[] applied in contexts)
            {
                foreach (string query in new[] { "s", "a", "co", "sp", "n", "p", "r", "t" })
                {
                    foreach (Suggestion suggestion in SearchCorpus.Suggest(query, applied))
                    {
                        SearchFilter[] withIt = applied.Concat(new[] { suggestion.AsFilter() }).ToArray();

                        Assert.NotEmpty(SearchCorpus.Filtered(withIt));
                    }
                }
            }
        }

        /// <summary>
        /// The value the plan asked the corpus to carry: one reachable only through another
        /// filter. Soccer exists on a single Genesis game, so it is offered inside that platform
        /// and absent outside it.
        /// </summary>
        [Fact]
        public void A_value_only_reachable_through_another_filter_appears_only_there()
        {
            Assert.Contains("Soccer", Values(SearchCorpus.Suggest("soc", SearchCorpus.Platform("Sega Genesis"))));
            Assert.DoesNotContain("Soccer", Values(SearchCorpus.Suggest("soc", SearchCorpus.Platform("Game Boy"))));
        }

        // ------------------------------------------------------------------
        // Suggestion ranking over realistic data - RULE-SEARCH-057, 058.
        // ------------------------------------------------------------------

        /// <summary>
        /// Match quality leads. "EA" is exactly a developer and only the start of a publisher, so
        /// the developer comes first even though both cover the same three games.
        /// </summary>
        [Fact]
        public void An_exact_value_leads_a_prefix_of_a_longer_one()
        {
            IReadOnlyList<Suggestion> offered = SearchCorpus.Suggest("ea");

            Assert.Equal("EA", offered[0].Value);
            Assert.Equal(ListCategoryType.Developer, offered[0].Facet);
            Assert.Equal(SuggestionMatch.ExactValue, offered[0].Match);

            Assert.Equal("EA Sports", offered[1].Value);
            Assert.Equal(SuggestionMatch.ValuePrefix, offered[1].Match);
        }

        /// <summary>
        /// The same word in two facets is two filters selecting different games, which is why the
        /// facet label beside every suggestion is required rather than decorative.
        /// </summary>
        [Fact]
        public void The_same_value_in_two_facets_is_offered_as_two_terms()
        {
            IReadOnlyList<Suggestion> offered = SearchCorpus.Suggest("sega");

            Assert.Contains(offered, s => s.Value == "Sega" && s.Facet == ListCategoryType.Publisher);
            Assert.Contains(offered, s => s.Value == "Sega" && s.Facet == ListCategoryType.Developer);

            Assert.NotEqual(SearchCorpus.Filtered(SearchCorpus.Publisher("Sega")).Count,
                            SearchCorpus.Filtered(SearchCorpus.Developer("Sega")).Count);
        }

        /// <summary>
        /// A whole-value prefix beats a word prefix however many games the word match covers -
        /// "Super Nintendo" carries as many games as the Nintendo developer term but is offered
        /// below it, because "nin" starts the one and not the other.
        /// </summary>
        [Fact]
        public void A_word_prefix_ranks_below_every_value_prefix()
        {
            IReadOnlyList<Suggestion> offered = SearchCorpus.Suggest("nin");

            int lastValuePrefix = offered.ToList().FindLastIndex(s => s.Match == SuggestionMatch.ValuePrefix);
            int firstTokenPrefix = offered.ToList().FindIndex(s => s.Match == SuggestionMatch.TokenPrefix);

            Assert.True(firstTokenPrefix > lastValuePrefix);
            Assert.Contains(offered, s => s.Value == "Super Nintendo");
        }

        /// <summary>
        /// RULE-SEARCH-058 over a realistic spread. A single letter matches values in every
        /// facet, and no one of them takes the list over.
        /// </summary>
        [Fact]
        public void A_broad_query_offers_a_cross_section_rather_than_one_facet()
        {
            IReadOnlyList<Suggestion> offered = SearchCorpus.Suggest("s");

            Assert.Equal(SearchSession.DefaultMaxSuggestions, offered.Count);

            Assert.True(offered.Select(s => s.Facet).Distinct().Count() >= 3);
            Assert.All(offered.GroupBy(s => s.Facet),
                       group => Assert.True(group.Count() <= SearchSession.DefaultMaxPerFacet));
        }

        [Fact]
        public void Suggestions_are_ordered_by_match_quality_then_by_count()
        {
            IReadOnlyList<Suggestion> offered = SearchCorpus.Suggest("s");

            for (int index = 1; index < offered.Count; index++)
            {
                Suggestion previous = offered[index - 1];
                Suggestion current = offered[index];

                if (previous.Match == current.Match)
                {
                    Assert.True(previous.Count >= current.Count);
                }
                else
                {
                    Assert.True(previous.Match > current.Match);
                }
            }
        }

        // ------------------------------------------------------------------
        // The algebra over realistic metadata - RULE-SEARCH-050, 051, 052.
        // ------------------------------------------------------------------

        /// <summary>
        /// Two genres accept either, like every other facet. Football is wholly inside Sports in
        /// this corpus, so a genre that is genuinely separate is used to show the widening.
        /// </summary>
        [Fact]
        public void Two_genres_accept_either()
        {
            IReadOnlyList<string> sports = SearchCorpus.Filtered(SearchCorpus.Genre("Sports"));
            IReadOnlyList<string> rpg = SearchCorpus.Filtered(SearchCorpus.Genre("RPG"));
            IReadOnlyList<string> either = SearchCorpus.Filtered(SearchCorpus.Genre("Sports"),
                                                                SearchCorpus.Genre("RPG"));

            Assert.Equal(sports.Count + rpg.Count, either.Count);
            Assert.All(sports, title => Assert.Contains(title, either));
            Assert.All(rpg, title => Assert.Contains(title, either));
        }

        /// <summary>
        /// A game has one platform, so two platform filters accept either. Requiring both would
        /// be an empty screen with nothing on it to explain why.
        /// </summary>
        [Fact]
        public void Two_platforms_accept_either()
        {
            IReadOnlyList<string> nes = SearchCorpus.Filtered(SearchCorpus.Platform("Nintendo Entertainment System"));
            IReadOnlyList<string> snes = SearchCorpus.Filtered(SearchCorpus.Platform("Super Nintendo"));
            IReadOnlyList<string> either = SearchCorpus.Filtered(
                SearchCorpus.Platform("Nintendo Entertainment System"), SearchCorpus.Platform("Super Nintendo"));

            Assert.Equal(nes.Count + snes.Count, either.Count);
        }

        [Fact]
        public void Platforms_widen_while_the_genre_across_them_narrows()
        {
            IReadOnlyList<string> rpgOnBoth = SearchCorpus.Filtered(
                SearchCorpus.Platform("Nintendo Entertainment System"),
                SearchCorpus.Platform("Super Nintendo"),
                SearchCorpus.Genre("RPG"));

            Assert.Contains("Chrono Trigger", rpgOnBoth);
            Assert.Contains("Final Fantasy", rpgOnBoth);
            Assert.DoesNotContain("Final Fantasy VII", rpgOnBoth);   // PlayStation
            Assert.DoesNotContain("Super Mario World", rpgOnBoth);   // SNES, not an RPG
        }

        // ------------------------------------------------------------------
        // Filters and typed text together - RULE-SEARCH-053.
        // ------------------------------------------------------------------

        [Fact]
        public void A_query_searches_within_the_filters()
        {
            Assert.Equal(new[] { "Final Fantasy VII", "Final Fantasy VIII", "Final Fantasy Tactics" }.OrderBy(t => t),
                         SearchCorpus.Search("final", SearchCorpus.Platform("Sony PlayStation")).OrderBy(t => t));
        }

        [Fact]
        public void A_query_matching_nothing_inside_the_filters_finds_nothing()
        {
            Assert.Empty(SearchCorpus.Search("zelda", SearchCorpus.Platform("Sega Genesis")));
        }

        /// <summary>
        /// The filters narrow what the text can reach, so the same query answers differently in
        /// different contexts - which is the whole point of the two applying together.
        /// </summary>
        [Fact]
        public void The_same_query_answers_differently_under_different_filters()
        {
            Assert.True(SearchCorpus.Search("sonic").Count
                        > SearchCorpus.Search("sonic", SearchCorpus.Platform("Sega Genesis")).Count);

            Assert.Empty(SearchCorpus.Search("sonic", SearchCorpus.Platform("Game Boy")));
        }

        // ------------------------------------------------------------------
        // Reaching "or" - RULE-SEARCH-052, 072.
        //
        // Every test the OR algebra had drove ToggleFilter directly, which is the API and not
        // the path a user has. Through the only path a user does have - typing, then selecting
        // a suggestion - a second platform could never be offered, because its count was taken
        // as an intersection with the survivors and two platform posting lists are disjoint by
        // construction. The algebra was right and unreachable for the whole of stage 4. These
        // go through Suggest so that cannot happen again quietly.
        // ------------------------------------------------------------------

        [Fact]
        public void A_second_platform_is_offered_once_one_is_applied()
        {
            IReadOnlyList<string> offered =
                Values(SearchCorpus.Suggest("sega", SearchCorpus.Platform("Nintendo Entertainment System")));

            Assert.Contains("Sega Genesis", offered);
        }

        /// <summary>
        /// The count of a widening filter is the union, not the intersection - so it is larger
        /// than the set the user is looking at, where every other suggestion's is smaller. That
        /// rise is the whole signal that this filter goes the other way, and the "or" on the chip
        /// is what explains it.
        /// </summary>
        [Fact]
        public void A_widening_suggestion_counts_what_it_would_add_rather_than_what_it_shares()
        {
            int nes = SearchCorpus.Filtered(SearchCorpus.Platform("Nintendo Entertainment System")).Count;
            int genesis = SearchCorpus.Filtered(SearchCorpus.Platform("Sega Genesis")).Count;

            Suggestion offered = SearchCorpus.Suggest("sega", SearchCorpus.Platform("Nintendo Entertainment System"))
                                             .Single(suggestion => suggestion.Value == "Sega Genesis");

            Assert.Equal(nes + genesis, offered.Count);
            Assert.True(offered.Count > nes);
        }

        /// <summary>
        /// And the number it promises is the number selecting it delivers. The count is the only
        /// preview a controller user gets, so a widening one that did not match the result set
        /// would be worse than no count at all.
        /// </summary>
        [Fact]
        public void The_widening_count_is_what_applying_it_actually_leaves()
        {
            Suggestion offered = SearchCorpus.Suggest("sega", SearchCorpus.Platform("Nintendo Entertainment System"))
                                             .Single(suggestion => suggestion.Value == "Sega Genesis");

            IReadOnlyList<string> both = SearchCorpus.Filtered(
                SearchCorpus.Platform("Nintendo Entertainment System"),
                SearchCorpus.Platform("Sega Genesis"));

            Assert.Equal(both.Count, offered.Count);
        }

        [Fact]
        public void A_second_release_year_is_offered_the_same_way()
        {
            IReadOnlyList<Suggestion> offered = SearchCorpus.Suggest("199", SearchCorpus.Year("1991"));

            Assert.Contains(offered, suggestion => suggestion.Facet == ListCategoryType.ReleaseYear
                                                   && suggestion.Value != "1991");
        }

        /// <summary>
        /// A widening count is the UNION, not the sum of the two sets - and this is the test the
        /// uniform algebra needed most.
        ///
        /// The widening count was written when only platform and release year ORed. A game
        /// carries one platform, so the games a new value reaches cannot already be among the
        /// survivors, and the count was simply added. Genres overlap constantly - this corpus has
        /// "Action, Adventure" seven times over - so adding would promise a number larger than
        /// applying the filter delivers, in the one place RULE-SEARCH-056 says the user has no
        /// other way to see what a press will do.
        ///
        /// Asserted as an invariant over several starting points rather than one worked example,
        /// because the failure is arithmetic and shows up wherever the sets happen to overlap.
        /// </summary>
        [Theory]
        [InlineData("Action", "a")]
        [InlineData("Sports", "o")]
        [InlineData("Platform", "a")]
        [InlineData("RPG", "s")]
        public void A_widening_count_is_what_applying_it_actually_leaves(string applied, string typed)
        {
            SearchFilter start = SearchCorpus.Genre(applied);

            foreach (Suggestion offered in SearchCorpus.Suggest(typed, start))
            {
                if (offered.Facet != ListCategoryType.Genre || offered.IsApplied)
                {
                    continue;
                }

                int actual = SearchCorpus.Filtered(start, SearchCorpus.Genre(offered.Value)).Count;

                Assert.True(actual == offered.Count,
                            $"{applied} + {offered.Value}: offered {offered.Count}, applying leaves {actual}");
            }
        }

        /// <summary>
        /// And the direction reversed from what it used to be: a second genre widens now, where
        /// under the old algebra it could only narrow.
        /// </summary>
        [Fact]
        public void A_second_genre_widens()
        {
            int sports = SearchCorpus.Filtered(SearchCorpus.Genre("Sports")).Count;

            foreach (Suggestion offered in SearchCorpus.Suggest("o", SearchCorpus.Genre("Sports")))
            {
                if (offered.Facet == ListCategoryType.Genre && !offered.IsApplied)
                {
                    Assert.True(offered.Count > sports,
                                $"{offered.Value} claims {offered.Count} against {sports} in Sports");
                }
            }
        }

        /// <summary>
        /// A widening term that would bring in nothing is not offered - the widening half of
        /// RULE-SEARCH-055. Not a dead end but a dead choice: it promises to widen and then
        /// leaves the screen exactly as it was.
        /// </summary>
        [Fact]
        public void A_widening_term_that_would_add_nothing_is_not_offered()
        {
            // Zelda is Nintendo-only in the corpus, so no Sega Genesis game survives it - and a
            // second platform that brings in none of its own games brings in nothing at all.
            IReadOnlyList<string> offered = Values(SearchCorpus.Suggest(
                "sega",
                SearchCorpus.Series("The Legend of Zelda"),
                SearchCorpus.Platform("Nintendo Entertainment System")));

            Assert.DoesNotContain("Sega Genesis", offered);
        }
    }
}
