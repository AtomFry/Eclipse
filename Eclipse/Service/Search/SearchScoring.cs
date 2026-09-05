using System;
using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// How well a game answers a query, as an ordered set of reasons rather than one number.
    ///
    /// The reasons are compared in order of authority, and a later one is only consulted when
    /// every earlier one has tied. That is the whole point: match quality decides, and the
    /// softer signals break ties beneath it. Nothing weak can outvote something strong, because
    /// they are never added together.
    ///
    /// Greater is better, so a descending sort puts the best first.
    /// </summary>
    public readonly struct SearchRank : IComparable<SearchRank>
    {
        /// <summary>A game that did not match every term. Ranks below everything that did.</summary>
        public static SearchRank NoMatch => default;

        public SearchRank(double termScore, int popularity, double brevity, bool matchedFirstToken)
        {
            IsMatch = true;
            TermScore = termScore;
            Popularity = popularity;
            Brevity = brevity;
            MatchedFirstToken = matchedFirstToken;
        }

        /// <summary>Whether the game matched every term of the query at all.</summary>
        public bool IsMatch { get; }

        /// <summary>
        /// How well the query's terms matched, averaged across them. The only signal allowed to
        /// dominate: an exact match beats a prefix beats a fuzzy one, whatever else is true.
        /// </summary>
        public double TermScore { get; }

        /// <summary>
        /// How well regarded and well played the game is, 0..SearchableGame.MaxPopularity.
        ///
        /// The first tiebreak, and the honest answer to "which of these did they mean". When a
        /// user types "zelda", nothing in the text says <em>The Legend of Zelda</em> is the one
        /// they want rather than <em>Zelda II</em> - both contain the word, and the sequel even
        /// starts with it. What distinguishes them is that one is the famous one, and the only
        /// evidence of that available here is the rating and the play count.
        /// </summary>
        public int Popularity { get; }

        /// <summary>
        /// How short the title is. The tiebreak beneath popularity, and the one that still works
        /// on a library with no ratings at all - "The Legend of Zelda" is four words where
        /// "Zelda II: The Adventure of Link" is six, so the base game wins either way.
        /// </summary>
        public double Brevity { get; }

        /// <summary>
        /// Whether the query matched the first word of the title.
        ///
        /// The weakest signal, and deliberately last. It used to be a flat fifteen points added
        /// into a single score, which made it larger than the whole popularity range and the
        /// whole brevity range combined - so it decided results it had no business deciding.
        /// </summary>
        public bool MatchedFirstToken { get; }

        public int CompareTo(SearchRank other)
        {
            if (IsMatch != other.IsMatch)
            {
                return IsMatch ? 1 : -1;
            }

            if (!IsMatch)
            {
                return 0;
            }

            int byTermScore = TermScore.CompareTo(other.TermScore);
            if (byTermScore != 0)
            {
                return byTermScore;
            }

            int byPopularity = Popularity.CompareTo(other.Popularity);
            if (byPopularity != 0)
            {
                return byPopularity;
            }

            int byBrevity = Brevity.CompareTo(other.Brevity);
            if (byBrevity != 0)
            {
                return byBrevity;
            }

            return MatchedFirstToken.CompareTo(other.MatchedFirstToken);
        }

        public override string ToString()
        {
            return IsMatch
                ? $"term {TermScore:0.##}, pop {Popularity}, brevity {Brevity:0.##}, first {MatchedFirstToken}"
                : "no match";
        }
    }

    /// <summary>
    /// Ranks a game against a query.
    ///
    /// Titles are short documents from a homogeneous corpus, so BM25's document-length and
    /// term-rarity machinery earns little here and costs predictability. What replaced it is not
    /// a cleverer formula but a different shape: the signals are ordered by authority and
    /// compared in turn, rather than weighted and summed.
    ///
    /// WHY NOT ONE NUMBER. It was one number, and the numbers were the problem. A flat
    /// fifteen-point bonus for matching the first word of a title is larger than the entire
    /// popularity range plus the entire brevity range, so it silently decided every result where
    /// the match quality tied - putting "Zelda II: The Adventure of Link" above "The Legend of
    /// Zelda" for the query "zelda", because the sequel happens to start with the word. No
    /// choice of weights fixes that class of problem in general; they only move which case is
    /// wrong. Comparing in order removes the possibility.
    ///
    /// The numbers that remain are still initial values. What is no longer up for tuning is
    /// whether a weak signal can outvote a strong one - it cannot, by construction.
    ///
    /// Pure. Strings and numbers - no index, no session, no screen.
    /// </summary>
    public static class SearchScoring
    {
        /// <summary>A query term that is exactly one of the game's terms.</summary>
        public const double ExactTermScore = 100;

        /// <summary>Floor for a term the user is part way through typing.</summary>
        public const double PrefixTermBaseScore = 80;

        /// <summary>
        /// Awarded to a prefix in proportion to how much of the term it covers, so that a longer
        /// prefix of a term is a better match - and so that a prefix covering the whole term
        /// scores the same as an exact match, which is what makes the last keystroke of a word
        /// not change the ranking.
        /// </summary>
        public const double PrefixTermLengthBonus = 20;

        /// <summary>The brevity score of a one word title. Falls to zero at BrevityTitleTokens.</summary>
        public const double MaxBrevity = 5;

        /// <summary>The title length at which brevity has fallen to nothing.</summary>
        public const int BrevityTitleTokens = 8;

        /// <summary>
        /// The game's rank for this query, or NoMatch if it does not match every term.
        ///
        /// Requiring every term is the AND: two terms narrow, they never widen, which is what
        /// every user expects from a search box - and the opposite of the voice path, where each
        /// recognised phrase produces a list of its own.
        /// </summary>
        public static SearchRank Rank(SearchableGame game, SearchQuery query)
        {
            if (game == null || query == null || query.IsEmpty)
            {
                return SearchRank.NoMatch;
            }

            double termScoreTotal = 0;
            bool matchedFirstToken = false;

            foreach (string term in query.CompleteTerms)
            {
                double termScore;
                int tokenIndex;

                if (!TryMatchTerm(game, term, false, out termScore, out tokenIndex))
                {
                    return SearchRank.NoMatch;
                }

                termScoreTotal += termScore;
                matchedFirstToken |= tokenIndex == 0;
            }

            if (query.PrefixTerm != null)
            {
                double termScore;
                int tokenIndex;

                if (!TryMatchTerm(game, query.PrefixTerm, true, out termScore, out tokenIndex))
                {
                    return SearchRank.NoMatch;
                }

                termScoreTotal += termScore;
                matchedFirstToken |= tokenIndex == 0;
            }

            // Averaged rather than summed so a two term query's scores stay on the same scale as
            // a one term query's. Ranking happens within a single query, so this changes no
            // ordering - it just keeps the numbers legible when a person reads a failing test.
            return new SearchRank(termScoreTotal / query.TermCount,
                                  game.Popularity,
                                  Brevity(game),
                                  matchedFirstToken);
        }

        /// <summary>
        /// The best score any of the game's tokens gets for this term, and which token earned
        /// it. False if none of them match at all.
        ///
        /// Ties go to the earliest token, so the first-token tiebreak sees the most favourable
        /// reading of a title that repeats a word.
        /// </summary>
        private static bool TryMatchTerm(SearchableGame game, string term, bool asPrefix,
                                         out double bestScore, out int bestTokenIndex)
        {
            bestScore = 0;
            bestTokenIndex = -1;

            IReadOnlyList<string> tokens = game.TitleTokens;

            for (int index = 0; index < tokens.Count; index++)
            {
                double score = TokenScore(tokens[index], term, asPrefix);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTokenIndex = index;
                }
            }

            return bestTokenIndex >= 0;
        }

        /// <summary>
        /// One token against one term.
        ///
        /// A token is findable by more than the way it is spelled in the title - "vii" is also
        /// indexed as 7 and seven - so scoring has to consider the same alternates the index
        /// used, or a game found through an alternate would score zero for the term that found
        /// it. The probe answers false for the overwhelming majority of tokens without
        /// allocating.
        /// </summary>
        private static double TokenScore(string token, string term, bool asPrefix)
        {
            IReadOnlyList<string> alternates;
            if (!TextAnalyzer.TryGetNumeralAlternates(token, out alternates))
            {
                return FormScore(token, term, asPrefix);
            }

            double best = 0;
            for (int index = 0; index < alternates.Count; index++)
            {
                double score = FormScore(alternates[index], term, asPrefix);
                if (score > best)
                {
                    best = score;
                }
            }

            return best;
        }

        private static double FormScore(string form, string term, bool asPrefix)
        {
            if (!asPrefix)
            {
                return string.Equals(form, term, StringComparison.Ordinal) ? ExactTermScore : 0;
            }

            if (!form.StartsWith(term, StringComparison.Ordinal))
            {
                return 0;
            }

            return PrefixTermBaseScore + (PrefixTermLengthBonus * term.Length / form.Length);
        }

        // Falls linearly from the whole of MaxBrevity at a one word title to nothing at
        // BrevityTitleTokens.
        private static double Brevity(SearchableGame game)
        {
            double ratio = (double)game.TitleTokens.Count / BrevityTitleTokens;

            if (ratio > 1)
            {
                ratio = 1;
            }

            return MaxBrevity * (1 - ratio);
        }
    }
}
