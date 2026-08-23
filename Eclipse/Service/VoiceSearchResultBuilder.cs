using Eclipse.Models;
using System.Collections.Generic;
using System.Linq;

namespace Eclipse.Service
{
    /// <summary>
    /// Turns what the recogniser heard into the ranked lists the user browses.
    ///
    /// One list per distinct phrase, named after what was said rather than after a category, and
    /// the whole of RULE-SEARCH-013 to RULE-SEARCH-016 in one place. This used to be the middle
    /// sixty lines of VoiceRecognitionState.RecognizeCompleted, wrapped in the same try/catch as
    /// the state transitions and the attract mode calls around it - so the rules most likely to
    /// be tuned were the ones hardest to find and impossible to exercise.
    ///
    /// Nothing here touches the view model, the state machine, the recogniser or a singleton.
    /// Given a list of phrases and something to look them up in, it is a pure function.
    /// </summary>
    public static class VoiceSearchResultBuilder
    {
        /// <summary>
        /// The lists to show, best first, or an empty list if nothing matched. Never null.
        /// </summary>
        public static List<GameList> Build(IReadOnlyList<RecognizedPhrase> recognizedPhrases,
                                           IVoicePhraseIndex index)
        {
            List<RankedList> ranked = new List<RankedList>();

            if (recognizedPhrases == null || recognizedPhrases.Count == 0 || index == null)
            {
                return new List<GameList>();
            }

            // RULE-SEARCH-013: the same phrase can be hypothesised several times, and only the
            // highest confidence counts.
            foreach (IGrouping<string, RecognizedPhrase> group in recognizedPhrases.GroupBy(recognizedPhrase => recognizedPhrase.Phrase))
            {
                string phrase = group.Key;
                float confidence = group.Max(recognizedPhrase => recognizedPhrase.Confidence);

                // the games this phrase matches - already one entry per game, carrying that
                // game's best match type for the phrase
                IReadOnlyList<VoiceMatch> voiceMatches = index.Lookup(phrase);

                // RULE-SEARCH-016: a phrase that matched nothing produces no list.
                if (voiceMatches.Count == 0)
                {
                    continue;
                }

                List<GameMatch> matches = new List<GameMatch>(voiceMatches.Count);

                foreach (VoiceMatch voiceMatch in voiceMatches)
                {
                    GameMatch match = GameMatch.CloneForVoiceResult(voiceMatch.Game, voiceMatch.MatchType, voiceMatch.ConvertedTitle);
                    match.SetupVoiceMatchPercentage(confidence, phrase);
                    matches.Add(match);
                }

                // RULE-SEARCH-014: within a phrase's list, best match first.
                List<GameMatch> ordered = matches.OrderByDescending(match => match.MatchPercentage).ToList();

                // The two numbers this list is ranked by, taken from the matches that were just
                // scored. They used to be computed properties on GameList, which meant every
                // browse category carried a pair of voice search concepts and the ranking rule
                // was split between this method and three property getters over there.
                int bestMatchPercentage = ordered[0].MatchPercentage;
                int longestMatchingTitle = ordered
                    .Where(match => match.MatchPercentage == bestMatchPercentage)
                    .Max(match => match.Game.Title.Length);

                // The phrase is the list's value as well as what it displays - these lists are
                // named after what was said rather than after a category - so it is set on both.
                GameList gameList = new GameList
                {
                    ListTypeValue = phrase,
                    ListDescription = phrase,
                    MatchingGames = ordered
                };

                ranked.Add(new RankedList(gameList, bestMatchPercentage, longestMatchingTitle));
            }

            // RULE-SEARCH-015: lists by best match, then by the longest title that achieved it -
            // which favours the more specific title where two score the same.
            return ranked
                .OrderByDescending(list => list.BestMatchPercentage)
                .ThenByDescending(list => list.LongestMatchingTitle)
                .Select(list => list.GameList)
                .ToList();
        }

        /// <summary>
        /// A finished list, with the two numbers it is ranked by carried beside it rather than
        /// recomputed off the list itself every time the sort compares a pair.
        /// </summary>
        private readonly struct RankedList
        {
            public RankedList(GameList gameList, int bestMatchPercentage, int longestMatchingTitle)
            {
                GameList = gameList;
                BestMatchPercentage = bestMatchPercentage;
                LongestMatchingTitle = longestMatchingTitle;
            }

            public GameList GameList { get; }
            public int BestMatchPercentage { get; }
            public int LongestMatchingTitle { get; }
        }
    }
}
