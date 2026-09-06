using System.Collections.Generic;
using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>One metadata value a game carries: which dimension, and what the value is.</summary>
    public readonly struct FacetValue
    {
        public FacetValue(ListCategoryType facet, string value)
        {
            Facet = facet;
            Value = value;
        }

        /// <summary>The dimension - genre, platform, publisher and so on.</summary>
        public ListCategoryType Facet { get; }

        /// <summary>The value, as the user sees it. "Sega Genesis", not a normalised form.</summary>
        public string Value { get; }

        public override string ToString()
        {
            return $"{Facet}: {Value}";
        }
    }

    /// <summary>
    /// One game's metadata, as the filter engine sees it.
    ///
    /// The facet half of what SearchableGame is for titles, and it exists for the same reason:
    /// everything below the boundary works on this rather than on IGame, so the index, the
    /// algebra and the ranking can all be exercised against object literals with no Big Box
    /// behind them.
    ///
    /// Kept separate from SearchableGame rather than folded into it. They are wanted at
    /// different times by different things - a title search needs no facets, and a facet filter
    /// needs no tokens - and combining them would make every title search carry metadata it does
    /// not read.
    ///
    /// See docs/plans/text-search-metadata-filters.md 7.1.
    /// </summary>
    public sealed class GameFacets
    {
        public GameFacets(int catalogIndex, IReadOnlyList<FacetValue> values)
        {
            CatalogIndex = catalogIndex;
            Values = values ?? new FacetValue[0];
        }

        /// <summary>
        /// The game's position in GameCatalog.Games - the same identity the title index uses, so
        /// a facet posting list and a title posting list can be intersected directly.
        /// </summary>
        public int CatalogIndex { get; }

        /// <summary>Every metadata value this game carries, across every facet.</summary>
        public IReadOnlyList<FacetValue> Values { get; }

        public override string ToString()
        {
            return $"[{CatalogIndex}] {Values.Count} values";
        }
    }
}
