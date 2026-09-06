using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// One row of a search result, described but not yet resolved: the constraints that row
    /// stands for, and what it is called.
    ///
    /// Not a list of games. Turning this into games costs a filter intersection and a title
    /// search, and the thing that owns the catalog does that - see TextSearchState. Kept apart so
    /// the question of <em>which rows a search has</em> can be answered, and tested, without a
    /// library, a catalog or a screen.
    /// </summary>
    public sealed class SearchRow
    {
        private static readonly SearchFilter[] NoFilters = new SearchFilter[0];

        public SearchRow(string query, IReadOnlyList<SearchFilter> filters, bool isPrimary)
        {
            Query = query ?? string.Empty;
            Filters = filters ?? NoFilters;
            IsPrimary = isPrimary;
        }

        /// <summary>The text this row searches for, or empty if it is the row that drops it.</summary>
        public string Query { get; }

        /// <summary>The filters this row applies. Same order as the chip row, minus any dropped one.</summary>
        public IReadOnlyList<SearchFilter> Filters { get; }

        /// <summary>
        /// Whether this is the search the user actually asked for, as against one of the near
        /// misses beneath it.
        /// </summary>
        public bool IsPrimary { get; }

        public bool HasQuery => Query.Length > 0;

        /// <summary>How many constraints this row carries - the query counts as one.</summary>
        public int ConstraintCount => Filters.Count + (HasQuery ? 1 : 0);

        /// <summary>
        /// What the list is called (RULE-SEARCH-076).
        ///
        /// Load-bearing rather than decorative: the search panel fades out while the cursor is
        /// down among the rows (RULE-SEARCH-069), so the heading is the only thing left telling
        /// one row from the next - and with several rows that is a job it did not have before.
        ///
        /// Only the primary is prefixed. A secondary row calling itself "Search:" would claim to
        /// be the search when it is explicitly the search with something taken out.
        /// </summary>
        public string Name
        {
            get
            {
                List<string> parts = new List<string>(ConstraintCount);

                if (HasQuery)
                {
                    parts.Add(Query);
                }

                foreach (SearchFilter filter in Filters)
                {
                    parts.Add(filter.Value);
                }

                string described = string.Join(" · ", parts);

                if (!IsPrimary)
                {
                    return described;
                }

                return described.Length == 0 ? "Search" : "Search: " + described;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// A row once it has been run: what it stands for, and the games it found.
    ///
    /// Still catalog indices rather than games - the projection to GameMatch needs the catalog
    /// and belongs above this boundary.
    /// </summary>
    public sealed class SearchRowResult
    {
        public SearchRowResult(SearchRow row, IReadOnlyList<SearchHit> hits)
        {
            Row = row;
            Hits = hits;
        }

        public SearchRow Row { get; }

        public IReadOnlyList<SearchHit> Hits { get; }

        /// <summary>What the list is called - see SearchRow.Name.</summary>
        public string Name => Row.Name;

        public int Count => Hits.Count;

        public bool IsPrimary => Row.IsPrimary;

        public override string ToString()
        {
            return $"{Name} ({Count})";
        }
    }

    /// <summary>
    /// Which rows a search produces.
    ///
    /// THE IDEA (RULE-SEARCH-073): stacked filters fail by over-narrowing, and when they do the
    /// user cannot tell which constraint cost them the games. So beneath the search they asked
    /// for, one row per constraint, each showing what the search would have been <em>without</em>
    /// that one. Every row is a near miss rather than a fresh browse.
    ///
    /// The alternative - one row per filter applied alone - was rejected in the plan: those rows
    /// are the same whatever else is applied, run to thousands of games on a real library, and
    /// answer "browse by genre" rather than "what did I over-narrow".
    ///
    /// Pure. Given text and filters it answers with rows. It does not know what a game is.
    /// </summary>
    public static class SearchRows
    {
        /// <summary>
        /// The rows, primary first.
        ///
        /// Below two constraints there are no secondary rows (RULE-SEARCH-075): leaving out the
        /// only constraint yields the unfiltered library, which is either everything or - with no
        /// query - a duplicate of the primary. The duplicate-row case is one Eclipse has already
        /// been bitten by; the fix is not to build the row.
        /// </summary>
        public static IReadOnlyList<SearchRow> For(string query, IReadOnlyList<SearchFilter> filters)
        {
            string text = query ?? string.Empty;
            IReadOnlyList<SearchFilter> applied = filters ?? new SearchFilter[0];

            List<SearchRow> rows = new List<SearchRow> { new SearchRow(text, applied, true) };

            int constraints = applied.Count + (text.Length > 0 ? 1 : 0);
            if (constraints < 2)
            {
                return rows;
            }

            // The query first, because free text is the likeliest of all the constraints to be
            // wrong - a typo narrows exactly as hard as a wrong year does, and this is the row
            // that rescues one. Then the filters most-recently-applied first, on the same
            // reasoning: the last thing added is the likeliest culprit for what just disappeared.
            if (text.Length > 0)
            {
                rows.Add(new SearchRow(string.Empty, applied, false));
            }

            for (int dropped = applied.Count - 1; dropped >= 0; dropped--)
            {
                rows.Add(new SearchRow(text, Without(applied, dropped), false));
            }

            return rows;
        }

        /// <summary>
        /// Every filter but one - by position, so a single filter is dropped rather than a whole
        /// facet. With "Sports and Football on NES" the rows are "Sports on NES" and "Football on
        /// NES", not "NES": dropping the facet would take out two constraints at once and stop
        /// the row being a near miss.
        /// </summary>
        private static IReadOnlyList<SearchFilter> Without(IReadOnlyList<SearchFilter> filters, int dropped)
        {
            List<SearchFilter> kept = new List<SearchFilter>(filters.Count - 1);

            for (int index = 0; index < filters.Count; index++)
            {
                if (index != dropped)
                {
                    kept.Add(filters[index]);
                }
            }

            return kept;
        }
    }
}
