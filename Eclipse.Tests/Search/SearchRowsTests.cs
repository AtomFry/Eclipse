using Eclipse.Models;
using Eclipse.Service.Search;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// Which rows a search has, and what they are called - RULE-SEARCH-073 … 076.
    ///
    /// Below the UI boundary entirely: SearchRows is given text and filters and answers with
    /// rows, so everything here can be asserted without a library, a catalog or a screen. What
    /// the rows resolve to is stage 5c's problem, and is deliberately not asked here.
    /// </summary>
    public class SearchRowsTests
    {
        private static SearchFilter Genre(string value) => new SearchFilter(ListCategoryType.Genre, value);
        private static SearchFilter Platform(string value) => new SearchFilter(ListCategoryType.Platform, value);
        private static SearchFilter Developer(string value) => new SearchFilter(ListCategoryType.Developer, value);
        private static SearchFilter Year(string value) => new SearchFilter(ListCategoryType.ReleaseYear, value);

        private static IReadOnlyList<string> Names(IEnumerable<SearchRow> rows)
        {
            return rows.Select(row => row.Name).ToList();
        }

        private static readonly SearchFilter[] FourFilters =
        {
            Developer("Capcom"),
            Genre("Shooter"),
            Platform("Nintendo Entertainment System"),
            Year("1988")
        };

        // ------------------------------------------------------------------
        // How many rows, and when - RULE-SEARCH-073, 075.
        // ------------------------------------------------------------------

        [Fact]
        public void A_search_with_nothing_applied_is_one_row()
        {
            IReadOnlyList<SearchRow> rows = SearchRows.For(string.Empty, new SearchFilter[0]);

            Assert.Single(rows);
            Assert.True(rows[0].IsPrimary);
        }

        [Fact]
        public void A_query_alone_is_one_row()
        {
            Assert.Single(SearchRows.For("sonic", new SearchFilter[0]));
        }

        /// <summary>
        /// RULE-SEARCH-075. Leaving out the only filter yields the whole library, which is not a
        /// near miss - and with no query it is the primary row over again. Eclipse has been bitten
        /// by duplicate rows once already; the fix is not to build the row.
        /// </summary>
        [Fact]
        public void A_single_filter_alone_is_one_row()
        {
            Assert.Single(SearchRows.For(string.Empty, new[] { Genre("Shooter") }));
        }

        [Fact]
        public void A_query_and_one_filter_are_two_constraints_and_so_fan_out()
        {
            IReadOnlyList<SearchRow> rows = SearchRows.For("sonic", new[] { Genre("Shooter") });

            Assert.Equal(3, rows.Count);
            Assert.Equal(new[] { "Search: sonic · Shooter", "Shooter", "sonic" }, Names(rows));
        }

        [Fact]
        public void Every_constraint_gets_a_row_that_leaves_it_out()
        {
            IReadOnlyList<SearchRow> rows = SearchRows.For("mega", FourFilters);

            Assert.Equal(1 + 1 + FourFilters.Length, rows.Count);
        }

        // ------------------------------------------------------------------
        // What each row contains - RULE-SEARCH-073, 074.
        // ------------------------------------------------------------------

        [Fact]
        public void The_primary_row_carries_everything_the_user_asked_for()
        {
            SearchRow primary = SearchRows.For("mega", FourFilters)[0];

            Assert.True(primary.IsPrimary);
            Assert.Equal("mega", primary.Query);
            Assert.Equal(FourFilters, primary.Filters);
        }

        /// <summary>
        /// RULE-SEARCH-074. A typo narrows exactly as hard as a wrong year does, so the text is a
        /// constraint like any other and gets a row that drops it - which is the row that rescues
        /// a misspelling without the user having to work out that they misspelled anything.
        /// </summary>
        [Fact]
        public void The_typed_query_is_a_constraint_and_gets_a_row_of_its_own()
        {
            IReadOnlyList<SearchRow> rows = SearchRows.For("mega", FourFilters);

            SearchRow withoutQuery = rows.Single(row => !row.IsPrimary && !row.HasQuery);

            Assert.Equal(FourFilters, withoutQuery.Filters);
        }

        [Fact]
        public void Each_secondary_row_drops_exactly_one_constraint()
        {
            foreach (SearchRow row in SearchRows.For("mega", FourFilters).Skip(1))
            {
                Assert.Equal(FourFilters.Length, row.ConstraintCount);
            }
        }

        /// <summary>
        /// Dropped by position, not by facet. Two genres are two constraints, so "Sports and
        /// Football on NES" gives "Sports on NES" and "Football on NES" - dropping the facet
        /// would take out both at once and the row would stop being a near miss.
        /// </summary>
        [Fact]
        public void Two_filters_of_one_facet_are_dropped_separately()
        {
            IReadOnlyList<SearchRow> rows = SearchRows.For(
                string.Empty,
                new[] { Genre("Sports"), Genre("Football"), Platform("Nintendo Entertainment System") });

            Assert.Equal(
                new[]
                {
                    "Search: Sports · Football · Nintendo Entertainment System",
                    "Sports · Football",
                    "Sports · Nintendo Entertainment System",
                    "Football · Nintendo Entertainment System"
                },
                Names(rows));
        }

        // ------------------------------------------------------------------
        // The order - most recently applied first.
        // ------------------------------------------------------------------

        /// <summary>
        /// The query leads because free text is the likeliest constraint to be wrong; the filters
        /// then run backwards because the last thing added is the likeliest culprit for whatever
        /// just disappeared. Both halves are the same rule: the most recent suspect first.
        /// </summary>
        [Fact]
        public void The_rows_run_from_the_most_recent_constraint_backwards()
        {
            Assert.Equal(
                new[]
                {
                    "Search: mega · Capcom · Shooter · Nintendo Entertainment System · 1988",
                    "Capcom · Shooter · Nintendo Entertainment System · 1988",
                    "mega · Capcom · Shooter · Nintendo Entertainment System",
                    "mega · Capcom · Shooter · 1988",
                    "mega · Capcom · Nintendo Entertainment System · 1988",
                    "mega · Shooter · Nintendo Entertainment System · 1988"
                },
                Names(SearchRows.For("mega", FourFilters)));
        }

        [Fact]
        public void Filters_keep_the_chip_rows_order_within_each_row()
        {
            SearchRow withoutTheYear = SearchRows.For(string.Empty, FourFilters)[1];

            Assert.Equal(
                new[] { "Capcom", "Shooter", "Nintendo Entertainment System" },
                withoutTheYear.Filters.Select(filter => filter.Value));
        }

        // ------------------------------------------------------------------
        // Naming - RULE-SEARCH-076.
        // ------------------------------------------------------------------

        [Fact]
        public void Only_the_primary_row_is_prefixed()
        {
            IReadOnlyList<SearchRow> rows = SearchRows.For("mega", FourFilters);

            Assert.StartsWith("Search: ", rows[0].Name);

            foreach (SearchRow row in rows.Skip(1))
            {
                Assert.DoesNotContain("Search", row.Name);
            }
        }

        /// <summary>
        /// The name the search screen has always shown with nothing typed. Pinned because this is
        /// now shared code, and the heading is the only thing on screen once the panel fades.
        /// </summary>
        [Fact]
        public void An_empty_search_is_still_called_Search()
        {
            Assert.Equal("Search", SearchRows.For(string.Empty, new SearchFilter[0])[0].Name);
        }

        [Fact]
        public void The_primary_name_reads_query_then_filters()
        {
            Assert.Equal(
                "Search: mega · Capcom · Shooter · Nintendo Entertainment System · 1988",
                SearchRows.For("mega", FourFilters)[0].Name);
        }

        [Fact]
        public void Null_inputs_are_treated_as_nothing_applied()
        {
            IReadOnlyList<SearchRow> rows = SearchRows.For(null, null);

            Assert.Single(rows);
            Assert.Equal("Search", rows[0].Name);
        }
    }
}
