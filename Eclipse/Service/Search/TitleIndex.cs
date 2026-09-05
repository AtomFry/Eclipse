using System;
using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// The inverted index: every term a game can be found by, and the games that carry it.
    ///
    /// This is the structural change that the plan's 5.1 is about. The voice grammar registers
    /// every contiguous run of words in every title, because System.Speech needs the set of
    /// recognisable utterances enumerated up front - a seven word title costs about thirty
    /// entries. Text search has no such constraint: the query arrives one keystroke at a time
    /// and can be intersected against the index at query time, so a seven word title costs
    /// seven postings and multi-word queries cost an intersection instead of a pre-enumeration.
    ///
    /// Postings are catalog indices rather than object references. GameCatalog already orders
    /// itself deterministically so that downstream sorts are reproducible, which makes position
    /// a stable identity - and int[] keeps a very large library's index to a few megabytes and
    /// makes intersection a linear merge.
    ///
    /// Pure library data. Built once from games; knows nothing about queries, sessions or
    /// screens.
    /// </summary>
    public sealed class TitleIndex : ISearchIndex
    {
        private static readonly int[] NoPostings = new int[0];

        // Terms sorted ordinal, with postings parallel to them. Sorted rather than hashed
        // because prefix matching needs a range, and a range is what a sorted array gives for
        // two binary searches - materialising every prefix of every term would multiply the
        // index size by the average term length for the same answers.
        private readonly string[] terms;
        private readonly int[][] postings;

        // Games by catalog index. Sparse-tolerant: an index the builder never saw is null.
        private readonly SearchableGame[] gamesByCatalogIndex;

        private TitleIndex(string[] terms, int[][] postings, SearchableGame[] gamesByCatalogIndex)
        {
            this.terms = terms;
            this.postings = postings;
            this.gamesByCatalogIndex = gamesByCatalogIndex;
        }

        /// <summary>How many distinct terms the library is findable by.</summary>
        public int TermCount => terms.Length;

        /// <summary>
        /// Builds the index. The expensive part is analysing titles, and this does it once per
        /// game; callers hold the result for the life of the library.
        /// </summary>
        public static TitleIndex Build(IReadOnlyList<SearchableGame> games)
        {
            Dictionary<string, List<int>> gamesByTerm = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            int highestCatalogIndex = -1;

            if (games != null)
            {
                foreach (SearchableGame game in games)
                {
                    if (game == null)
                    {
                        continue;
                    }

                    if (game.CatalogIndex > highestCatalogIndex)
                    {
                        highestCatalogIndex = game.CatalogIndex;
                    }

                    foreach (string token in game.TitleTokens)
                    {
                        // one token can be findable by several terms - "vii" is also 7 and seven
                        foreach (string term in TextAnalyzer.IndexTerms(token))
                        {
                            List<int> carrying;
                            if (!gamesByTerm.TryGetValue(term, out carrying))
                            {
                                carrying = new List<int>();
                                gamesByTerm.Add(term, carrying);
                            }

                            carrying.Add(game.CatalogIndex);
                        }
                    }
                }
            }

            string[] sortedTerms = new string[gamesByTerm.Count];
            gamesByTerm.Keys.CopyTo(sortedTerms, 0);
            Array.Sort(sortedTerms, StringComparer.Ordinal);

            int[][] sortedPostings = new int[sortedTerms.Length][];
            for (int index = 0; index < sortedTerms.Length; index++)
            {
                sortedPostings[index] = SortedDistinct(gamesByTerm[sortedTerms[index]]);
            }

            SearchableGame[] byCatalogIndex = new SearchableGame[highestCatalogIndex + 1];
            if (games != null)
            {
                foreach (SearchableGame game in games)
                {
                    if (game != null && game.CatalogIndex >= 0)
                    {
                        byCatalogIndex[game.CatalogIndex] = game;
                    }
                }
            }

            return new TitleIndex(sortedTerms, sortedPostings, byCatalogIndex);
        }

        public IReadOnlyList<int> Exact(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return NoPostings;
            }

            int found = Array.BinarySearch(terms, term, StringComparer.Ordinal);
            return found >= 0 ? postings[found] : NoPostings;
        }

        /// <summary>
        /// Every game carrying a term that starts with this prefix. This is what an as-you-type
        /// query is: two binary searches for the range of matching terms, then the union of what
        /// they point at.
        /// </summary>
        public IReadOnlyList<int> Prefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return NoPostings;
            }

            int first = LowerBound(prefix);
            int last = first;
            while (last < terms.Length && terms[last].StartsWith(prefix, StringComparison.Ordinal))
            {
                last++;
            }

            if (last == first)
            {
                return NoPostings;
            }

            // The common case once a few characters have been typed: the prefix reaches exactly
            // one term, so its postings are already the answer and there is nothing to merge.
            if (last - first == 1)
            {
                return postings[first];
            }

            List<int> union = new List<int>();
            for (int index = first; index < last; index++)
            {
                union.AddRange(postings[index]);
            }

            return SortedDistinct(union);
        }

        public SearchableGame Game(int catalogIndex)
        {
            if (catalogIndex < 0 || catalogIndex >= gamesByCatalogIndex.Length)
            {
                return null;
            }

            return gamesByCatalogIndex[catalogIndex];
        }

        // The first position whose term is not less than the prefix. Everything matching the
        // prefix starts here, because a term with the prefix sorts at or after the prefix
        // itself under an ordinal comparison.
        private int LowerBound(string prefix)
        {
            int low = 0;
            int high = terms.Length;

            while (low < high)
            {
                int middle = low + ((high - low) / 2);

                if (string.CompareOrdinal(terms[middle], prefix) < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        // Postings are kept sorted and distinct so that intersection is a linear merge and a
        // game cannot be counted twice for carrying a term in two of its tokens.
        private static int[] SortedDistinct(List<int> values)
        {
            if (values.Count == 0)
            {
                return NoPostings;
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
