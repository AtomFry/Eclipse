using System;
using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>One game a query matched, and how well. A value, not an object.</summary>
    public readonly struct SearchHit
    {
        public SearchHit(int catalogIndex, SearchRank rank)
        {
            CatalogIndex = catalogIndex;
            Rank = rank;
        }

        /// <summary>The game's position in GameCatalog.Games.</summary>
        public int CatalogIndex { get; }

        /// <summary>Why it ranked where it did. Carried so ranking can be seen and tested.</summary>
        public SearchRank Rank { get; }

        public override string ToString()
        {
            return $"{CatalogIndex} ({Rank})";
        }
    }

    /// <summary>
    /// Turns a query into ranked games.
    ///
    /// A pure function over an index: given the same query and the same index it returns the
    /// same list, in the same order, every time. It holds no state, touches no singleton, and
    /// knows nothing about the session, the keyboard or the screen - which is what lets the
    /// whole of search ranking be exercised with no Big Box behind it.
    ///
    /// Results are catalog indices with their scores. Projecting those back to games, rows or
    /// anything a person can look at happens at the boundary, in one place - see
    /// docs/plans/text-search.md 7.6.
    /// </summary>
    public static class SearchEngine
    {
        /// <summary>
        /// How many results the live row materialises by default. The row shows thirteen at a
        /// time and nobody scrolls two hundred box arts, so this is generous rather than tight.
        /// </summary>
        public const int DefaultMaxResults = 200;

        private static readonly SearchHit[] NoHits = new SearchHit[0];
        private static readonly int[] NoCandidates = new int[0];

        /// <summary>
        /// The games matching every term of the query, best first. Empty, never null.
        /// </summary>
        /// <param name="maxResults">
        /// The most to return. Zero or less means all of them - which is what committing a
        /// search wants, where the whole ranked set becomes a list to browse.
        /// </param>
        public static IReadOnlyList<SearchHit> Search(SearchQuery query, ISearchIndex index,
                                                      int maxResults = DefaultMaxResults)
        {
            if (query == null || query.IsEmpty || index == null)
            {
                return NoHits;
            }

            IReadOnlyList<int> candidates = Candidates(query, index);
            if (candidates.Count == 0)
            {
                return NoHits;
            }

            List<SearchHit> hits = new List<SearchHit>(candidates.Count);

            foreach (int catalogIndex in candidates)
            {
                SearchableGame game = index.Game(catalogIndex);
                if (game == null)
                {
                    continue;
                }

                SearchRank rank = SearchScoring.Rank(game, query);

                // The index said this game carries every term, so a non-match here means the two
                // disagree - a game whose posting was written by a term its title cannot be
                // scored against. Skipped rather than ranked last, because a result the user
                // cannot see the reason for is worse than one that is missing.
                if (rank.IsMatch)
                {
                    hits.Add(new SearchHit(catalogIndex, rank));
                }
            }

            // Best first, then by catalog index - which is the library's own sort-title order,
            // so games that rank identically come out in a stable, meaningful sequence rather
            // than in whatever order the intersection produced.
            hits.Sort(CompareHits);

            if (maxResults > 0 && hits.Count > maxResults)
            {
                return hits.GetRange(0, maxResults);
            }

            return hits;
        }

        // Rank descending - SearchRank compares with greater meaning better - then catalog index
        // ascending, so a pair that ties on every ranking signal is still ordered the same way
        // every time.
        private static int CompareHits(SearchHit left, SearchHit right)
        {
            int byRank = right.Rank.CompareTo(left.Rank);
            return byRank != 0 ? byRank : left.CatalogIndex.CompareTo(right.CatalogIndex);
        }

        /// <summary>
        /// The games carrying every term, as ascending catalog indices.
        ///
        /// This is the AND, and it is the whole reason the index stores words rather than
        /// phrases: a multi-word query is answered by intersecting posting lists at query time
        /// instead of by having enumerated every word run when the index was built.
        /// </summary>
        private static IReadOnlyList<int> Candidates(SearchQuery query, ISearchIndex index)
        {
            List<IReadOnlyList<int>> postings = new List<IReadOnlyList<int>>(query.TermCount);

            foreach (string term in query.CompleteTerms)
            {
                IReadOnlyList<int> carrying = index.Exact(term);
                if (carrying.Count == 0)
                {
                    return NoCandidates;
                }

                postings.Add(carrying);
            }

            if (query.PrefixTerm != null)
            {
                IReadOnlyList<int> carrying = index.Prefix(query.PrefixTerm);
                if (carrying.Count == 0)
                {
                    return NoCandidates;
                }

                postings.Add(carrying);
            }

            if (postings.Count == 0)
            {
                return NoCandidates;
            }

            // Smallest first: the result can only be as large as the shortest list, so starting
            // there keeps every subsequent merge walking the smallest sequence available.
            postings.Sort(CompareByLength);

            IReadOnlyList<int> intersection = postings[0];
            for (int index2 = 1; index2 < postings.Count && intersection.Count > 0; index2++)
            {
                intersection = Intersect(intersection, postings[index2]);
            }

            return intersection;
        }

        private static int CompareByLength(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            return left.Count.CompareTo(right.Count);
        }

        // Both sides are ascending and distinct - TitleIndex guarantees it - so this is a linear
        // merge rather than a lookup per element.
        private static int[] Intersect(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            List<int> both = new List<int>(Math.Min(left.Count, right.Count));

            int leftIndex = 0;
            int rightIndex = 0;

            while (leftIndex < left.Count && rightIndex < right.Count)
            {
                int leftValue = left[leftIndex];
                int rightValue = right[rightIndex];

                if (leftValue == rightValue)
                {
                    both.Add(leftValue);
                    leftIndex++;
                    rightIndex++;
                }
                else if (leftValue < rightValue)
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
    }
}
