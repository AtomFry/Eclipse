using System;
using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// How well a game answers a query, as one number.
    ///
    /// Titles are short documents from a homogeneous corpus, so BM25's document-length and
    /// term-rarity machinery earns little here and costs predictability. A fixed additive table
    /// is easier to tune, easier to explain, and - because it is all in one place and all pure -
    /// easy to pin with tests.
    ///
    /// THE CONSTANTS BELOW ARE INITIAL VALUES, NOT A RANKING MODEL. They are a starting point
    /// chosen to be reasonable and, more importantly, to be tunable with evidence. Expect
    /// several to be wrong on a real library: exact match may prove too dominant, the first
    /// token bonus too strong, brevity may do odd things to collections and compilations, and
    /// popularity may turn out not to belong in a search ranking at all. Discovering that is
    /// what SearchCorpusTests is for. See docs/plans/text-search.md 5.5.
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

        /// <summary>
        /// For matching the start of the title. "Sonic the Hedgehog" should beat a game that
        /// merely mentions Sonic later on.
        /// </summary>
        public const double FirstTokenBonus = 15;

        /// <summary>
        /// Awarded in proportion to how many query terms matched.
        ///
        /// Under the strict AND of Match() this is presently constant: a game that failed to
        /// match a term scores zero and never reaches here, so the proportion is always 1 and
        /// the bonus is always the whole of it. It is kept because it is in the specification
        /// and because it is the hook partial matching would need - not because it currently
        /// separates anything. It cannot affect ordering today.
        /// </summary>
        public const double CoverageBonus = 10;

        /// <summary>
        /// For being the short title rather than the long one. "Sonic the Hedgehog 2" should
        /// beat "Sonic the Hedgehog 2 Special Edition Collection".
        /// </summary>
        public const double BrevityBonus = 5;

        /// <summary>The title length at which the brevity bonus has fallen to nothing.</summary>
        public const int BrevityTitleTokens = 8;

        /// <summary>
        /// The game's score for this query, or zero if it does not match every term.
        ///
        /// Zero is the AND: all query terms must match. Two terms narrow, they never widen,
        /// which is what every user expects from a search box - and the opposite of the voice
        /// path, where each recognised phrase produces a list of its own.
        /// </summary>
        public static double Score(SearchableGame game, SearchQuery query)
        {
            if (game == null || query == null || query.IsEmpty)
            {
                return 0;
            }

            double termScoreTotal = 0;
            int matchedTerms = 0;
            bool matchedFirstToken = false;

            foreach (string term in query.CompleteTerms)
            {
                double termScore;
                int tokenIndex;

                if (!TryMatchTerm(game, term, false, out termScore, out tokenIndex))
                {
                    return 0;
                }

                termScoreTotal += termScore;
                matchedTerms++;
                matchedFirstToken |= tokenIndex == 0;
            }

            if (query.PrefixTerm != null)
            {
                double termScore;
                int tokenIndex;

                if (!TryMatchTerm(game, query.PrefixTerm, true, out termScore, out tokenIndex))
                {
                    return 0;
                }

                termScoreTotal += termScore;
                matchedTerms++;
                matchedFirstToken |= tokenIndex == 0;
            }

            // Averaged rather than summed so a two term query's scores stay on the same scale as
            // a one term query's. Ranking happens within a single query, so this changes no
            // ordering - it just keeps the numbers legible when a person reads a failing test.
            double score = termScoreTotal / query.TermCount;

            if (matchedFirstToken)
            {
                score += FirstTokenBonus;
            }

            score += CoverageBonus * matchedTerms / query.TermCount;
            score += Brevity(game);
            score += game.Popularity;

            return score;
        }

        /// <summary>
        /// The best score any of the game's tokens gets for this term, and which token earned
        /// it. False if none of them match at all.
        ///
        /// Ties go to the earliest token, because the tie-break that matters is the first-token
        /// bonus.
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

        // Falls linearly from the whole bonus at a one word title to nothing at BrevityTitleTokens.
        private static double Brevity(SearchableGame game)
        {
            double ratio = (double)game.TitleTokens.Count / BrevityTitleTokens;

            if (ratio > 1)
            {
                ratio = 1;
            }

            return BrevityBonus * (1 - ratio);
        }
    }
}
