using System;
using System.Collections.Generic;
using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// One metadata filter the user has applied.
    ///
    /// Called a filter rather than a chip: a chip is how one of these is <em>drawn</em>, and
    /// nothing below the view is allowed to know that a screen exists. The plan names the file
    /// SearchChip; this is the same thing under the name that survives the boundary.
    /// </summary>
    public readonly struct SearchFilter : IEquatable<SearchFilter>
    {
        public SearchFilter(ListCategoryType facet, string value)
        {
            Facet = facet;
            Value = value;
        }

        public ListCategoryType Facet { get; }

        public string Value { get; }

        /// <summary>
        /// Two filters are the same filter if they name the same value of the same facet.
        /// Compared case-insensitively, because that is how FacetIndex groups values - so a
        /// filter cannot be applied twice by arriving with different capitalisation.
        /// </summary>
        public bool Equals(SearchFilter other)
        {
            return Facet == other.Facet
                   && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is SearchFilter && Equals((SearchFilter)obj);
        }

        public override int GetHashCode()
        {
            int valueHash = Value == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
            return ((int)Facet * 397) ^ valueHash;
        }

        public override string ToString()
        {
            return $"{Facet}: {Value}";
        }
    }

    /// <summary>How a filter combines with the one before it in the chip row.</summary>
    public enum FilterJoin
    {
        /// <summary>The first filter. Nothing to join to.</summary>
        None,

        /// <summary>Both must hold. Filters of different facets (RULE-SEARCH-050).</summary>
        And,

        /// <summary>Either will do. Two values of the same facet (RULE-SEARCH-051).</summary>
        Or
    }

    /// <summary>
    /// One applied filter as the chip row shows it: the filter, and how it reads against the one
    /// before it.
    ///
    /// The joining word exists because "or" is the half of the algebra that surprises. Two things
    /// side by side already read as both, so only the OR is drawn (RULE-SEARCH-067) - between two
    /// filters of one facet, and nowhere else.
    /// </summary>
    public readonly struct AppliedFilter
    {
        public AppliedFilter(SearchFilter filter, FilterJoin join)
        {
            Filter = filter;
            Join = join;
        }

        public SearchFilter Filter { get; }

        public FilterJoin Join { get; }

        public ListCategoryType Facet => Filter.Facet;

        public string Value => Filter.Value;

        public override string ToString()
        {
            return Join == FilterJoin.None ? Filter.ToString() : $"{Join.ToString().ToLowerInvariant()} {Filter}";
        }
    }

    /// <summary>
    /// The games surviving a set of applied filters.
    ///
    /// THE ALGEBRA, and the one part of the filter feature where getting it wrong produces a
    /// result set that is silently always empty:
    ///
    ///   * Filters of <b>different</b> facets combine with AND.               (RULE-SEARCH-050)
    ///   * Filters of the <b>same</b> facet combine with OR - every facet.    (RULE-SEARCH-051)
    ///
    ///     (Platform1 OR Platform2) AND (Genre1 OR Genre2) AND (Developer1 OR Developer2)
    ///
    /// A second value of a facet therefore always widens (RULE-SEARCH-052), which is what makes
    /// a same-facet filter impossible to turn into an empty screen. The uniformity is the point:
    /// there is no per-facet knowledge to carry, here or in the user's head. See the note in
    /// FacetIndex for what the earlier split cost and the measurement that ended it.
    ///
    /// Pure. It is given the selected filters and an index and returns catalog indices; it knows
    /// nothing about how the filters were chosen, or by whom.
    /// </summary>
    public static class FilterSet
    {
        private static readonly int[] NoGames = new int[0];

        /// <summary>
        /// The games surviving every filter, ascending - or <b>null</b> when nothing is filtered,
        /// meaning "no constraint".
        ///
        /// Null rather than a list of every game in the library, because the common case is no
        /// filters at all and materialising the whole catalog to say "everything survived" is
        /// work done on every keystroke to express nothing. Callers intersect with it only when
        /// it is there.
        /// </summary>
        public static int[] Apply(IReadOnlyList<SearchFilter> filters, FacetIndex index)
        {
            if (filters == null || filters.Count == 0 || index == null)
            {
                return null;
            }

            // Group by facet first: the combining rule is decided per facet, and only the
            // results of those are intersected together.
            Dictionary<ListCategoryType, List<int[]>> byFacet = new Dictionary<ListCategoryType, List<int[]>>();

            foreach (SearchFilter filter in filters)
            {
                List<int[]> postings;
                if (!byFacet.TryGetValue(filter.Facet, out postings))
                {
                    postings = new List<int[]>();
                    byFacet.Add(filter.Facet, postings);
                }

                // A filter naming a value the library does not have contributes an empty list,
                // which is correct: it can only narrow to nothing.
                postings.Add(index.Games(filter.Facet, filter.Value));
            }

            int[] surviving = null;

            foreach (KeyValuePair<ListCategoryType, List<int[]>> facet in byFacet)
            {
                // OR within the facet, always. Two values of one facet mean either.
                int[] withinFacet = UnionAll(facet.Value);

                surviving = surviving == null ? withinFacet : Intersect(surviving, withinFacet);

                if (surviving.Length == 0)
                {
                    return NoGames;
                }
            }

            return surviving ?? NoGames;
        }

        /// <summary>
        /// The games surviving every filter <em>except</em> those of one facet - or null when
        /// nothing else constrains, with the same meaning Apply gives it.
        ///
        /// This is what makes a second value of an already-filtered facet countable. Such a filter
        /// widens rather than narrows (RULE-SEARCH-052), so "how many games would this leave"
        /// cannot be answered by intersecting with the current survivors - the answer lies
        /// partly outside them. Taking the facet out and asking what the rest of the filters
        /// leave gives the ground the union is measured against.
        /// </summary>
        public static int[] ApplyWithout(IReadOnlyList<SearchFilter> filters, FacetIndex index, ListCategoryType facet)
        {
            if (filters == null || filters.Count == 0)
            {
                return null;
            }

            List<SearchFilter> rest = new List<SearchFilter>(filters.Count);

            foreach (SearchFilter filter in filters)
            {
                if (filter.Facet != facet)
                {
                    rest.Add(filter);
                }
            }

            return Apply(rest, index);
        }

        /// <summary>
        /// Whether a filter is already applied. Selecting an applied term removes it rather than
        /// adding it again (RULE-SEARCH-061), so duplicates are unreachable rather than
        /// deduplicated later.
        /// </summary>
        public static bool Contains(IReadOnlyList<SearchFilter> filters, SearchFilter filter)
        {
            if (filters == null)
            {
                return false;
            }

            foreach (SearchFilter applied in filters)
            {
                if (applied.Equals(filter))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// How many games two ascending, distinct lists have in common.
        ///
        /// Counting rather than materialising, because this is what a suggestion's count is and
        /// it is computed for every candidate on every keystroke. Nothing needs the games
        /// themselves until one is actually selected.
        /// </summary>
        /// <summary>
        /// How lopsided two lists have to be before searching beats merging. Eight is well past
        /// the break-even and far short of needing to be tuned.
        /// </summary>
        private const int SearchWhenSmallerBy = 8;

        public static int IntersectCount(int[] left, int[] right)
        {
            if (left == null || right == null || left.Length == 0 || right.Length == 0)
            {
                return 0;
            }

            // A merge walks both lists end to end, which is right when they are a similar size
            // and badly wrong when they are not. A metadata term carrying ten games, counted
            // against a filter set of five thousand, costs ten binary searches rather than five
            // thousand steps - and this runs for every candidate term on every keystroke, so the
            // difference is the difference between 60 ms and 2 ms.
            if (left.Length * SearchWhenSmallerBy < right.Length)
            {
                return CountBySearching(left, right);
            }

            if (right.Length * SearchWhenSmallerBy < left.Length)
            {
                return CountBySearching(right, left);
            }

            int count = 0;
            int leftIndex = 0;
            int rightIndex = 0;

            while (leftIndex < left.Length && rightIndex < right.Length)
            {
                if (left[leftIndex] == right[rightIndex])
                {
                    count++;
                    leftIndex++;
                    rightIndex++;
                }
                else if (left[leftIndex] < right[rightIndex])
                {
                    leftIndex++;
                }
                else
                {
                    rightIndex++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts by looking each of the shorter list's values up in the longer one.
        ///
        /// The search window only ever moves forward: both lists are ascending, so once a value
        /// has been found or passed, nothing earlier can match again. That makes the whole pass
        /// O(small × log large) at worst and close to O(small) in practice.
        /// </summary>
        private static int CountBySearching(int[] small, int[] large)
        {
            int count = 0;
            int from = 0;

            foreach (int value in small)
            {
                if (from >= large.Length)
                {
                    break;
                }

                int found = System.Array.BinarySearch(large, from, large.Length - from, value);

                if (found >= 0)
                {
                    count++;
                    from = found + 1;
                }
                else
                {
                    from = ~found;
                }
            }

            return count;
        }

        private static int[] IntersectAll(List<int[]> postings)
        {
            // Smallest first: the result can only be as small as the shortest list.
            postings.Sort(CompareByLength);

            int[] result = postings[0];
            for (int index = 1; index < postings.Count && result.Length > 0; index++)
            {
                result = Intersect(result, postings[index]);
            }

            return result;
        }

        private static int[] UnionAll(List<int[]> postings)
        {
            int[] result = postings[0];
            for (int index = 1; index < postings.Count; index++)
            {
                result = Union(result, postings[index]);
            }

            return result;
        }

        private static int CompareByLength(int[] left, int[] right)
        {
            return left.Length.CompareTo(right.Length);
        }

        // Both sides ascending and distinct - FacetIndex guarantees it - so these are linear
        // merges rather than a lookup per element.

        private static int[] Intersect(int[] left, int[] right)
        {
            List<int> both = new List<int>(Math.Min(left.Length, right.Length));

            int leftIndex = 0;
            int rightIndex = 0;

            while (leftIndex < left.Length && rightIndex < right.Length)
            {
                if (left[leftIndex] == right[rightIndex])
                {
                    both.Add(left[leftIndex]);
                    leftIndex++;
                    rightIndex++;
                }
                else if (left[leftIndex] < right[rightIndex])
                {
                    leftIndex++;
                }
                else
                {
                    rightIndex++;
                }
            }

            return both.ToArray();
        }

        private static int[] Union(int[] left, int[] right)
        {
            List<int> either = new List<int>(left.Length + right.Length);

            int leftIndex = 0;
            int rightIndex = 0;

            while (leftIndex < left.Length && rightIndex < right.Length)
            {
                if (left[leftIndex] == right[rightIndex])
                {
                    either.Add(left[leftIndex]);
                    leftIndex++;
                    rightIndex++;
                }
                else if (left[leftIndex] < right[rightIndex])
                {
                    either.Add(left[leftIndex]);
                    leftIndex++;
                }
                else
                {
                    either.Add(right[rightIndex]);
                    rightIndex++;
                }
            }

            while (leftIndex < left.Length)
            {
                either.Add(left[leftIndex++]);
            }

            while (rightIndex < right.Length)
            {
                either.Add(right[rightIndex++]);
            }

            return either.ToArray();
        }
    }
}
