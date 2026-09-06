using System;
using System.Collections.Generic;
using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>How well a typed query matched a metadata value. Greater is better.</summary>
    public enum SuggestionMatch
    {
        None = 0,

        /// <summary>The query is the start of one of the value's words. "sports" finds "EA Sports".</summary>
        TokenPrefix = 1,

        /// <summary>The query is the start of the whole value. "ea spo" finds "EA Sports".</summary>
        ValuePrefix = 2,

        /// <summary>The query is the whole value.</summary>
        ExactValue = 3
    }

    /// <summary>
    /// One metadata term offered to the user, and what selecting it would do.
    /// </summary>
    public sealed class Suggestion
    {
        public Suggestion(FacetTerm term, SuggestionMatch match, int count, bool isApplied)
        {
            Term = term;
            Match = match;
            Count = count;
            IsApplied = isApplied;
        }

        public FacetTerm Term { get; }

        /// <summary>The value as the user sees it - "Sega Genesis", "EA Sports".</summary>
        public string Value => Term.Value;

        /// <summary>Which dimension it belongs to. Not decoration - "Sonic" the series and
        /// "Sonic Team" the developer are different filters, and without the label the user
        /// cannot tell which one they are about to apply.</summary>
        public ListCategoryType Facet => Term.Facet;

        public SuggestionMatch Match { get; }

        /// <summary>
        /// How many games selecting this would leave.
        ///
        /// Load-bearing, not display polish. On a controller there is no hover, no tooltip and
        /// no way to inspect anything, so this is the entire preview mechanism - it is what
        /// turns a list of guesses into a list of consequences, and what makes stacked filtering
        /// safe to explore rather than a gamble that costs several presses to undo.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Whether this filter is already applied. Selecting it then removes it, so a duplicate
        /// is unreachable rather than deduplicated later (RULE-SEARCH-061).
        /// </summary>
        public bool IsApplied { get; }

        /// <summary>The filter selecting this would apply or remove.</summary>
        public SearchFilter AsFilter()
        {
            return new SearchFilter(Term.Facet, Term.Value);
        }

        public override string ToString()
        {
            return $"{Value} ({Facet}, {Count})";
        }
    }

    /// <summary>
    /// Which metadata terms to offer for what the user has typed.
    ///
    /// THE INVARIANT THAT MAKES THIS FEEL INTELLIGENT (RULE-SEARCH-054/055/056): counts are
    /// computed against the <em>current filter set</em>, and a term that would change nothing is
    /// never offered. Together those make it impossible to reach a pointless choice by picking
    /// suggestions - the user cannot select "Football" after "Sports" unless a sports football
    /// game exists, because it simply is not there. The screen never has to explain a dead end
    /// because it never creates one.
    ///
    /// COUNTED IN THE DIRECTION THE FILTER ACTUALLY GOES. Most filters narrow, so their count is
    /// an intersection with the survivors. A second value of a single-valued facet does not:
    /// platforms and years OR (RULE-SEARCH-052), so it widens, and its count lies partly outside
    /// the current set. Counting it as a narrowing is not merely wrong by a few games - two
    /// platform posting lists are disjoint by construction, so the intersection is always zero
    /// and RULE-SEARCH-055 then drops the term. That is what made "or" unreachable through the
    /// UI for the whole of stage 4: the algebra was right, fully tested, and no user could ever
    /// produce it, because nothing could ever offer them the second platform.
    ///
    /// Treat that as an invariant, not an optimisation target. The obvious way to make this
    /// cheaper - rank against the whole library and filter afterwards, or cache across chip
    /// changes - breaks it. If it ever needs to be faster, make the intersection faster.
    ///
    /// Pure. Given text, an index and what is already applied, it answers with terms and counts.
    /// It does not know that anything will be applied afterwards, and it returns no view models.
    /// </summary>
    public static class SuggestionRanker
    {
        private static readonly Suggestion[] None = new Suggestion[0];

        /// <summary>
        /// The terms worth offering, best first.
        /// </summary>
        /// <param name="queryText">
        /// The raw query buffer. Analysed here the same way titles are, so what the user types
        /// and what the metadata says are compared on equal terms.
        /// </param>
        /// <param name="filterSet">
        /// The games surviving the filters already applied, or null for no constraint. Counts are
        /// computed against this and <b>not</b> against the typed text, because selecting a
        /// suggestion clears the query (RULE-SEARCH-060) - so a count that included the text
        /// would promise a number that selecting it does not deliver. Widening terms are counted
        /// against this too, as the part of the union that already survives.
        /// </param>
        public static IReadOnlyList<Suggestion> Rank(string queryText,
                                                     FacetIndex index,
                                                     IReadOnlyList<SearchFilter> applied,
                                                     int[] filterSet,
                                                     int maxSuggestions,
                                                     int maxPerFacet)
        {
            if (index == null || maxSuggestions <= 0)
            {
                return None;
            }

            string query = TextAnalyzer.Normalize(queryText);
            if (query.Length == 0)
            {
                return None;
            }

            List<Suggestion> candidates = new List<Suggestion>();

            // Which single-valued facets already carry a filter. A term of one of those is a
            // WIDENING rather than a narrowing (RULE-SEARCH-052), and counting it as a narrowing
            // is what made "or" unreachable: two platform posting lists are disjoint by
            // construction, so the intersection is always zero and the term was always dropped.
            HashSet<ListCategoryType> widening = WideningFacets(applied);
            Dictionary<ListCategoryType, int[]> withoutFacet = new Dictionary<ListCategoryType, int[]>();
            int survivingCount = filterSet == null ? 0 : filterSet.Length;

            // A library has a few thousand distinct metadata values at most, and a prefix test
            // over that costs far less than a keystroke's budget - so this scans rather than
            // maintaining a second term index. If that stops being true the answer is an index
            // like the title side has, not a different shape here.
            foreach (FacetTerm term in index.Terms)
            {
                SuggestionMatch match = MatchOf(term, query);
                if (match == SuggestionMatch.None)
                {
                    continue;
                }

                bool isApplied = FilterSet.Contains(applied, new SearchFilter(term.Facet, term.Value));

                int count;

                if (!isApplied && widening.Contains(term.Facet))
                {
                    // The games this would ADD. Everything already surviving stays - a union
                    // only grows - so the total is what survives now plus what this brings in
                    // from outside. The two are disjoint by construction, because a game carries
                    // exactly one value of a single-valued facet, which is why this adds rather
                    // than needing the union materialised.
                    int[] ground = Ground(withoutFacet, applied, index, term.Facet);

                    int added = ground == null
                        ? term.GameCount
                        : FilterSet.IntersectCount(term.Games, ground);

                    // The widening equivalent of RULE-SEARCH-055. Not a dead end but a dead
                    // choice: a term that would bring in nothing promises to widen and then
                    // leaves the screen exactly as it was.
                    if (added == 0)
                    {
                        continue;
                    }

                    count = survivingCount + added;
                }
                else
                {
                    count = filterSet == null
                        ? term.GameCount
                        : FilterSet.IntersectCount(term.Games, filterSet);

                    // RULE-SEARCH-055 - a term that would produce nothing is never offered. An
                    // applied term is kept whatever its count, so the user can always see what
                    // they have on and take it off again.
                    if (count == 0 && !isApplied)
                    {
                        continue;
                    }
                }

                candidates.Add(new Suggestion(term, match, count, isApplied));
            }

            if (candidates.Count == 0)
            {
                return None;
            }

            // RULE-SEARCH-057 - match quality first, then how many games it would leave. Ordered
            // rather than weighted, for the same reason the title ranking is: a big count should
            // not be able to lift a poor match above a good one.
            candidates.Sort(Compare);

            return Cap(candidates, maxSuggestions, maxPerFacet);
        }

        /// <summary>
        /// The single-valued facets that already carry a filter - the ones where another value
        /// would widen the search rather than narrow it.
        /// </summary>
        private static HashSet<ListCategoryType> WideningFacets(IReadOnlyList<SearchFilter> applied)
        {
            HashSet<ListCategoryType> facets = new HashSet<ListCategoryType>();

            if (applied == null)
            {
                return facets;
            }

            foreach (SearchFilter filter in applied)
            {
                if (Facets.IsSingleValued(filter.Facet))
                {
                    facets.Add(filter.Facet);
                }
            }

            return facets;
        }

        /// <summary>
        /// What the other filters leave, with one facet lifted out - the ground a widening term
        /// is measured against.
        ///
        /// Cached per facet for the length of one ranking pass. A library can offer hundreds of
        /// candidate terms per keystroke but only a handful of facets, so this is a few
        /// intersections per keystroke rather than a few hundred - which is what keeps the
        /// widening path inside the same budget the narrowing one was tuned to.
        /// </summary>
        private static int[] Ground(Dictionary<ListCategoryType, int[]> cache,
                                    IReadOnlyList<SearchFilter> applied,
                                    FacetIndex index,
                                    ListCategoryType facet)
        {
            int[] ground;
            if (cache.TryGetValue(facet, out ground))
            {
                return ground;
            }

            ground = FilterSet.ApplyWithout(applied, index, facet);
            cache.Add(facet, ground);
            return ground;
        }

        /// <summary>
        /// How the typed text matched a value, or None.
        ///
        /// Token prefix earns its place because users type the distinctive word rather than the
        /// first one: "sports" has to reach "EA Sports" and "genesis" has to reach "Sega
        /// Genesis". A multi-word query cannot match a single token, which is correct - at that
        /// point the user is spelling out the whole value.
        /// </summary>
        private static SuggestionMatch MatchOf(FacetTerm term, string query)
        {
            string value = term.NormalizedValue;

            if (string.Equals(value, query, StringComparison.Ordinal))
            {
                return SuggestionMatch.ExactValue;
            }

            if (value.StartsWith(query, StringComparison.Ordinal))
            {
                return SuggestionMatch.ValuePrefix;
            }

            foreach (string token in term.Tokens)
            {
                if (token.StartsWith(query, StringComparison.Ordinal))
                {
                    return SuggestionMatch.TokenPrefix;
                }
            }

            return SuggestionMatch.None;
        }

        private static int Compare(Suggestion left, Suggestion right)
        {
            int byMatch = right.Match.CompareTo(left.Match);
            if (byMatch != 0)
            {
                return byMatch;
            }

            int byCount = right.Count.CompareTo(left.Count);
            if (byCount != 0)
            {
                return byCount;
            }

            // Deterministic beneath the ranking signals, so a list of equal candidates does not
            // reshuffle between keystrokes.
            int byFacet = Facets.Order(left.Facet).CompareTo(Facets.Order(right.Facet));
            return byFacet != 0
                ? byFacet
                : string.Compare(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Takes the best, in two passes (RULE-SEARCH-058).
        ///
        /// The first pass allows no more than a few from any one facet. That is the half that
        /// matters: a library with two hundred developers whose names start with "ea" would
        /// otherwise fill the whole list with developers and bury the one genre the user was
        /// after, so the cap keeps the list a cross-section rather than a slice of the largest
        /// facet.
        ///
        /// The second pass then fills whatever slots are left over, in rank order and ignoring
        /// the cap. Without it the rule harms the user in the case it was never aimed at: when
        /// every candidate belongs to one facet there is nothing to crowd out, and holding the
        /// list to three would hide useful terms to protect diversity that cannot exist.
        /// Diversity is a floor, not a ceiling.
        /// </summary>
        private static IReadOnlyList<Suggestion> Cap(List<Suggestion> ranked, int maxSuggestions, int maxPerFacet)
        {
            List<Suggestion> taken = new List<Suggestion>(maxSuggestions);
            HashSet<FacetTerm> used = new HashSet<FacetTerm>();
            Dictionary<ListCategoryType, int> perFacet = new Dictionary<ListCategoryType, int>();

            foreach (Suggestion suggestion in ranked)
            {
                if (taken.Count == maxSuggestions)
                {
                    return taken;
                }

                int fromFacet;
                perFacet.TryGetValue(suggestion.Facet, out fromFacet);

                if (maxPerFacet > 0 && fromFacet >= maxPerFacet)
                {
                    continue;
                }

                taken.Add(suggestion);
                used.Add(suggestion.Term);
                perFacet[suggestion.Facet] = fromFacet + 1;
            }

            foreach (Suggestion suggestion in ranked)
            {
                if (taken.Count == maxSuggestions)
                {
                    break;
                }

                if (!used.Contains(suggestion.Term))
                {
                    taken.Add(suggestion);
                }
            }

            return taken;
        }
    }
}
