using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// Where a search gets its index, and whether there is one yet.
    ///
    /// The seam between the session and the service that builds the index. It exists so
    /// SearchSession can be exercised against a hand-built index, an index that is still
    /// preparing, and one that failed - three states that are otherwise only reachable by timing
    /// a real Big Box startup.
    ///
    /// Two members rather than one because "not ready" is a state the screen has to be able to
    /// say something about. A null index with no reason attached is how a feature ends up
    /// appearing to do nothing, which is what M-6 records.
    /// </summary>
    public interface ISearchIndexSource
    {
        /// <summary>Whether a search can run right now, and if not, why not.</summary>
        SearchAvailability Availability { get; }

        /// <summary>The index, or null unless Availability is Ready.</summary>
        ISearchIndex Index { get; }
    }
}
