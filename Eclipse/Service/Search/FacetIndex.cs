using System;
using System.Collections.Generic;
using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// Which facets can be filtered on, and how each one behaves.
    ///
    /// The single-valued/multi-valued split is not a style choice - it is inherited from the
    /// data, and getting it wrong produces a filter that can never match anything. A game has
    /// many genres and many publishers, but exactly one platform and one release year, so
    /// requiring two platforms at once is a guaranteed empty set. GameCatalog.BuildCategoryIndex
    /// already draws the same line: the single-valued facets go through a plain ToLookup, the
    /// rest through Expand.
    /// </summary>
    public static class Facets
    {
        /// <summary>
        /// The facets a search can filter on, in the order suggestions consider them.
        ///
        /// Mirrors GameListBuilder.MoreLikeThisCategories, so the two features agree on what
        /// counts as the most relevant kind of metadata.
        ///
        /// Release year and play mode were the two candidates for removal - both are plausibly
        /// noise, and the suggestion list is capped, so a facet nobody filters on costs a real
        /// slot. Confirmed useful in stage 4e and staying. Playlist is the one facet deliberately
        /// absent; see SearchableGameProjection.FacetsOf.
        /// </summary>
        public static readonly IReadOnlyList<ListCategoryType> Filterable = new[]
        {
            ListCategoryType.Series,
            ListCategoryType.Genre,
            ListCategoryType.Platform,
            ListCategoryType.Publisher,
            ListCategoryType.Developer,
            ListCategoryType.PlayMode,
            ListCategoryType.ReleaseYear
        };

        /// <summary>
        /// Whether a game can carry only one value for this facet.
        ///
        /// Decides how two filters on the same facet combine: single-valued facets OR, because
        /// AND could never match; multi-valued facets AND, because a game genuinely carrying
        /// both is meaningful and is what the feature was asked for.
        /// </summary>
        public static bool IsSingleValued(ListCategoryType facet)
        {
            return facet == ListCategoryType.Platform || facet == ListCategoryType.ReleaseYear;
        }

        /// <summary>
        /// Where a facet sits in Filterable, or its count for one that is not filtered on. Used
        /// as a deterministic tiebreak so a list of equally ranked suggestions does not reshuffle
        /// between keystrokes.
        /// </summary>
        public static int Order(ListCategoryType facet)
        {
            for (int index = 0; index < Filterable.Count; index++)
            {
                if (Filterable[index] == facet)
                {
                    return index;
                }
            }

            return Filterable.Count;
        }
    }

    /// <summary>
    /// One metadata value, and every game carrying it.
    /// </summary>
    public sealed class FacetTerm
    {
        public FacetTerm(ListCategoryType facet, string value, IReadOnlyList<string> tokens, int[] games)
        {
            Facet = facet;
            Value = value;
            Tokens = tokens;
            Games = games;
            NormalizedValue = string.Join(" ", tokens);
        }

        public ListCategoryType Facet { get; }

        /// <summary>The value as the user sees it - "Sega Genesis", "EA Sports".</summary>
        public string Value { get; }

        /// <summary>
        /// The analysed tokens of the value, for matching what the user typed.
        ///
        /// Tokens rather than one normalised string, because users type the distinctive word
        /// rather than the first one: "sports" has to find "EA Sports" and "genesis" has to find
        /// "Sega Genesis".
        /// </summary>
        public IReadOnlyList<string> Tokens { get; }

        /// <summary>
        /// The tokens rejoined - what a whole-value match compares against.
        ///
        /// Held rather than built on demand because matching walks every term in the library on
        /// every keystroke, and joining ten thousand strings each time is the single largest
        /// avoidable cost in a suggestion pass.
        /// </summary>
        public string NormalizedValue { get; }

        /// <summary>Catalog indices carrying this value, ascending and distinct.</summary>
        public int[] Games { get; }

        /// <summary>How many games carry it. What a suggestion would show before it is applied.</summary>
        public int GameCount => Games.Length;

        public override string ToString()
        {
            return $"{Facet}: {Value} ({GameCount})";
        }
    }

    /// <summary>
    /// Every metadata value in the library, and the games carrying each.
    ///
    /// LIBRARY DATA, NOT SESSION DATA. Built once from the catalog and valid for every search
    /// that will ever run. It knows what terms exist; it does not know which are selected, which
    /// are being offered, or that a screen exists. The moment a FacetTerm grows an IsSelected or
    /// IsOffered field it stops being shareable and stops being testable independently of a
    /// session - and the change that does it will look like a one-line convenience. Selection
    /// belongs on the session; this is told what is applied, it does not remember.
    ///
    /// See docs/plans/text-search-metadata-filters.md 7.5.
    /// </summary>
    public sealed class FacetIndex
    {
        private static readonly int[] NoGames = new int[0];
        private static readonly FacetTerm[] NoTerms = new FacetTerm[0];

        // Terms by facet, then by value. Two levels because every question asked of this index
        // is either "all the terms" or "the games carrying this specific value".
        private readonly Dictionary<ListCategoryType, Dictionary<string, FacetTerm>> byFacet;

        private FacetIndex(Dictionary<ListCategoryType, Dictionary<string, FacetTerm>> byFacet,
                           IReadOnlyList<FacetTerm> terms)
        {
            this.byFacet = byFacet;
            Terms = terms;
        }

        /// <summary>An index over nothing. Filters applied to it survive rather than throwing.</summary>
        public static FacetIndex Empty { get; } =
            new FacetIndex(new Dictionary<ListCategoryType, Dictionary<string, FacetTerm>>(), NoTerms);

        /// <summary>
        /// Every term in the library, ordered by facet - following Facets.Filterable - and by
        /// value within each facet.
        ///
        /// A flat list rather than a search structure: a library has a few thousand distinct
        /// metadata values at most, and scanning that with a prefix test costs far less than a
        /// keystroke's budget. If that ever stops being true the answer is a term index like the
        /// title side has, not a different shape here.
        /// </summary>
        public IReadOnlyList<FacetTerm> Terms { get; }

        public int TermCount => Terms.Count;

        /// <summary>
        /// Builds the index. Values are grouped case-insensitively, because LaunchBox metadata is
        /// hand-entered and "Sega Genesis" and "Sega genesis" are the same platform; the first
        /// spelling encountered is the one displayed.
        /// </summary>
        public static FacetIndex Build(IReadOnlyList<GameFacets> games)
        {
            Dictionary<ListCategoryType, Dictionary<string, List<int>>> collected =
                new Dictionary<ListCategoryType, Dictionary<string, List<int>>>();

            // The display spelling to use for each value, which is the first one seen.
            Dictionary<ListCategoryType, Dictionary<string, string>> display =
                new Dictionary<ListCategoryType, Dictionary<string, string>>();

            if (games != null)
            {
                foreach (GameFacets game in games)
                {
                    if (game == null)
                    {
                        continue;
                    }

                    foreach (FacetValue value in game.Values)
                    {
                        if (string.IsNullOrWhiteSpace(value.Value))
                        {
                            continue;
                        }

                        AddValue(collected, display, game.CatalogIndex, value);
                    }
                }
            }

            return Publish(collected, display);
        }

        private static void AddValue(Dictionary<ListCategoryType, Dictionary<string, List<int>>> collected,
                                     Dictionary<ListCategoryType, Dictionary<string, string>> display,
                                     int catalogIndex,
                                     FacetValue value)
        {
            Dictionary<string, List<int>> valuesInFacet;
            if (!collected.TryGetValue(value.Facet, out valuesInFacet))
            {
                valuesInFacet = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
                collected.Add(value.Facet, valuesInFacet);
                display.Add(value.Facet, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            }

            string trimmed = value.Value.Trim();

            List<int> carrying;
            if (!valuesInFacet.TryGetValue(trimmed, out carrying))
            {
                carrying = new List<int>();
                valuesInFacet.Add(trimmed, carrying);
                display[value.Facet].Add(trimmed, trimmed);
            }

            carrying.Add(catalogIndex);
        }

        private static FacetIndex Publish(Dictionary<ListCategoryType, Dictionary<string, List<int>>> collected,
                                          Dictionary<ListCategoryType, Dictionary<string, string>> display)
        {
            Dictionary<ListCategoryType, Dictionary<string, FacetTerm>> byFacet =
                new Dictionary<ListCategoryType, Dictionary<string, FacetTerm>>();

            List<FacetTerm> ordered = new List<FacetTerm>();

            // Facet order follows Facets.Filterable so that Terms is stable and meaningful;
            // anything the catalog produced that is not filterable is simply not indexed.
            foreach (ListCategoryType facet in Facets.Filterable)
            {
                Dictionary<string, List<int>> valuesInFacet;
                if (!collected.TryGetValue(facet, out valuesInFacet))
                {
                    continue;
                }

                List<string> values = new List<string>(valuesInFacet.Keys);
                values.Sort(StringComparer.OrdinalIgnoreCase);

                Dictionary<string, FacetTerm> terms =
                    new Dictionary<string, FacetTerm>(StringComparer.OrdinalIgnoreCase);

                foreach (string value in values)
                {
                    FacetTerm term = new FacetTerm(facet,
                                                   display[facet][value],
                                                   TextAnalyzer.Tokenize(value),
                                                   SortedDistinct(valuesInFacet[value]));

                    terms.Add(value, term);
                    ordered.Add(term);
                }

                byFacet.Add(facet, terms);
            }

            return new FacetIndex(byFacet, ordered);
        }

        /// <summary>
        /// The games carrying one value, ascending. Empty if nothing carries it - or if the
        /// facet is not one search filters on.
        /// </summary>
        public int[] Games(ListCategoryType facet, string value)
        {
            FacetTerm term = Term(facet, value);
            return term == null ? NoGames : term.Games;
        }

        /// <summary>The term for one value, or null.</summary>
        public FacetTerm Term(ListCategoryType facet, string value)
        {
            Dictionary<string, FacetTerm> terms;
            if (value == null || !byFacet.TryGetValue(facet, out terms))
            {
                return null;
            }

            FacetTerm term;
            return terms.TryGetValue(value.Trim(), out term) ? term : null;
        }

        /// <summary>Every term in one facet, value-ordered. Empty if the facet has none.</summary>
        public IReadOnlyList<FacetTerm> TermsIn(ListCategoryType facet)
        {
            List<FacetTerm> terms = new List<FacetTerm>();

            foreach (FacetTerm term in Terms)
            {
                if (term.Facet == facet)
                {
                    terms.Add(term);
                }
            }

            return terms;
        }

        // Posting lists are kept sorted and distinct so intersection is a linear merge, and so a
        // game listing the same genre twice cannot be counted twice.
        private static int[] SortedDistinct(List<int> values)
        {
            if (values.Count == 0)
            {
                return NoGames;
            }

            values.Sort();

            int written = 1;
            for (int read = 1; read < values.Count; read++)
            {
                if (values[read] != values[written - 1])
                {
                    values[written] = values[read];
                    written++;
                }
            }

            int[] distinct = new int[written];
            values.CopyTo(0, distinct, 0, written);
            return distinct;
        }
    }
}
