using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// What evaluating a query needs from the index, and nothing else.
    ///
    /// Deliberately narrow, for the same reason IVoicePhraseIndex and IGameCatalogSource are: it
    /// lets SearchEngine be exercised against a handful of made-up games instead of a real
    /// library behind a real Big Box.
    ///
    /// Everything here is library data. The index knows which games carry which terms and what
    /// those games are; it does not know what the user has typed, what is selected, or that a
    /// screen exists - see docs/plans/text-search.md 7.6. Game() is on this interface rather
    /// than threaded separately because a SearchableGame is library data too, and splitting the
    /// two would mean every caller carrying a pair that must not disagree.
    ///
    /// The posting lists are the index's own, handed out rather than copied - a query reads
    /// several of them per keystroke and copying each would be pure waste. Callers read them and
    /// do not modify them.
    /// </summary>
    public interface ISearchIndex
    {
        /// <summary>
        /// The games carrying this term exactly, as ascending catalog indices. Empty, never
        /// null.
        /// </summary>
        IReadOnlyList<int> Exact(string term);

        /// <summary>
        /// The games carrying any term that starts with this prefix, as ascending catalog
        /// indices, deduplicated across the terms in the range. Empty, never null.
        /// </summary>
        IReadOnlyList<int> Prefix(string prefix);

        /// <summary>The game at a catalog index, or null if the index holds no such game.</summary>
        SearchableGame Game(int catalogIndex);
    }
}
