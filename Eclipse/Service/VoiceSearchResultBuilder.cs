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
            List<GameList> results = new List<GameList>();

            if (recognizedPhrases == null || recognizedPhrases.Count == 0 || index == null)
            {
                return results;
            }

            // RULE-SEARCH-013: the same phrase can be hypothesised several times, and only the
            // highest confidence counts.
            //
            // The phrase is the list's value as well as what it displays - these lists are named
            // after what was said rather than after a category - so it is set on both, and the
            // phrase is read back from the value.
            List<GameList> gameListsByPhrase = recognizedPhrases
                .GroupBy(recognizedPhrase => recognizedPhrase.Phrase)
                .Select(group => new GameList
                {
                    ListTypeValue = group.Key,
                    ListDescription = group.Key,
                    Confidence = group.Max(recognizedPhrase => recognizedPhrase.Confidence)
                })
                .ToList();

            foreach (GameList gameList in gameListsByPhrase)
            {
                // the games this phrase matches - already one entry per game, carrying that
                // game's best match type for the phrase
                IReadOnlyList<VoiceMatch> voiceMatches = index.Lookup(gameList.ListTypeValue);

                // RULE-SEARCH-016: a phrase that matched nothing produces no list.
                if (voiceMatches.Count == 0)
                {
                    continue;
                }

                List<GameMatch> matches = new List<GameMatch>(voiceMatches.Count);

                foreach (VoiceMatch voiceMatch in voiceMatches)
                {
                    GameMatch match = GameMatch.CloneForVoiceResult(voiceMatch.Game, voiceMatch.MatchType, voiceMatch.ConvertedTitle);
                    match.SetupVoiceMatchPercentage(gameList.Confidence, gameList.ListTypeValue);
                    matches.Add(match);
                }

                // RULE-SEARCH-014: within a phrase's list, best match first.
                gameList.MatchingGames = matches.OrderByDescending(match => match.MatchPercentage).ToList();

                results.Add(gameList);
            }

            // RULE-SEARCH-015: lists by best match, then by the longest title that achieved it -
            // which favours the more specific title where two score the same.
            return results
                .OrderByDescending(list => list.MaxMatchPercentage)
                .ThenByDescending(list => list.MaxTitleLength)
                .ToList();
        }
    }
}
