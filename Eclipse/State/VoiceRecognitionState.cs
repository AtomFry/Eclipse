using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Service;
using Eclipse.Models;

namespace Eclipse.State
{
    public class VoiceRecognitionState : EclipseState
    {
        private SpeechRecognizer speechRecognizer;
        private EclipseStateContext EclipseStateContext;
        private readonly AttractModeService attractModeService;

        public VoiceRecognitionState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            EclipseStateContext = eclipseStateContext;

            // the recogniser is built in the background after startup, so it may not be
            // ready yet - say so rather than appearing to do nothing
            VoiceSearchAvailability availability = SpeechRecognizerService.Instance.Availability;
            if (availability != VoiceSearchAvailability.Ready)
            {
                ShowUnavailable(availability);
                return;
            }

            speechRecognizer = SpeechRecognizerService.Instance.GetRecognizer();

            DoRecognize();
        }

        private void ShowUnavailable(VoiceSearchAvailability availability)
        {
            string message;
            switch (availability)
            {
                case VoiceSearchAvailability.Preparing:
                    message = "Voice search is still getting ready - please try again in a moment";
                    break;

                case VoiceSearchAvailability.Failed:
                    message = SpeechRecognizerService.Instance.FailureMessage ?? "Voice search is not available";
                    break;

                default:
                    message = "Voice search is turned off in the Eclipse settings";
                    break;
            }

            EclipseStateContext.MainWindowViewModel.IsRecognizing = false;

            DisplayingErrorState displayingErrorState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
            displayingErrorState.ErrorMessage = message;

            EclipseStateContext.TransitionToState(displayingErrorState);
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            speechRecognizer?.TryCancelRecognition();
            EclipseStateContext.MainWindowViewModel.IsRecognizing = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        public void DoRecognize()
        {
            // bail out if the recognizer didn't get setup properly
            if (speechRecognizer == null)
            {
                EclipseStateContext.MainWindowViewModel.IsRecognizing = false;
                EclipseStateContext.TransitionToState(EclipseStateContext.GetState(typeof(SelectingGameState)));
                return;
            }

            try
            {
                attractModeService.StopAttractMode();

                // stop any video or animations
                EclipseStateContext.MainWindowViewModel.CallStopVideoAndAnimationsFunction();

                EclipseStateContext.MainWindowViewModel.IsRecognizing = true;

                speechRecognizer.DoSpeechRecognition(RecognizeCompleted);
            }
            catch(Exception ex)
            {
                EclipseStateContext.MainWindowViewModel.IsRecognizing = false;

                DisplayingErrorState displayingErrorState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
                displayingErrorState.ErrorMessage = ex.Message;

                EclipseStateContext.TransitionToState(displayingErrorState);
            }
        }

        public void RecognizeCompleted(SpeechRecognizerResult speechRecognizerResult)
        {
            attractModeService.RestartAttractMode();

            if (!string.IsNullOrWhiteSpace(speechRecognizerResult.ErrorMessage))
            {
                EclipseStateContext.MainWindowViewModel.IsRecognizing = false;

                DisplayingErrorState displayingErrorState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
                displayingErrorState.ErrorMessage = speechRecognizerResult.ErrorMessage;
                EclipseStateContext.TransitionToState(displayingErrorState);
                return;
            }

            // a cancelled recognition is not a search that found nothing - go back to
            // browsing and leave the current lists exactly as they were
            if (speechRecognizerResult.Cancelled)
            {
                EclipseStateContext.MainWindowViewModel.IsRecognizing = false;
                EclipseStateContext.TransitionToState(EclipseStateContext.GetState(typeof(SelectingGameState)));
                return;
            }

            try
            {
                List<GameList> voiceRecognitionResults = new List<GameList>();

                // speech hypothesized adds phrases that it heard to the TempGameLists collection
                // for each phrase, get the set of matching games from the GameTitlePhrases dictionary
                if (speechRecognizerResult?.RecognizedPhrases?.Count() > 0)
                {
                    // in case the same phrase was recognized multiple times, group by phrase and keep only the max confidence.
                    // The phrase is the list's value as well as what it displays - these lists are named after what was
                    // said rather than after a category - so it is set on both, and the phrase is read back from the value.
                    List<GameList> distinctGameLists = speechRecognizerResult?.RecognizedPhrases
                        .GroupBy(s => s.Phrase)
                        .Select(s => new GameList { ListTypeValue = s.Key, ListDescription = s.Key, Confidence = s.Max(m => m.Confidence) }).ToList();

                    // loop through the gamelists (one list for each hypothesized phrase)
                    foreach (GameList gameList in distinctGameLists)
                    {
                        // get the games this phrase matches - already one entry per game,
                        // carrying that game's best match type for the phrase
                        IReadOnlyList<VoiceMatch> voiceMatches = VoiceSearchIndex.Instance.Lookup(gameList.ListTypeValue);

                        if (voiceMatches.Count > 0)
                        {
                            List<GameMatch> matches = new List<GameMatch>(voiceMatches.Count);

                            foreach (VoiceMatch voiceMatch in voiceMatches)
                            {
                                GameMatch match = GameMatch.CloneForVoiceResult(voiceMatch.Game, voiceMatch.MatchType, voiceMatch.ConvertedTitle);
                                match.SetupVoiceMatchPercentage(gameList.Confidence, gameList.ListTypeValue);
                                matches.Add(match);
                            }

                            gameList.MatchingGames = matches.OrderByDescending(match => match.MatchPercentage).ToList();
                            voiceRecognitionResults.Add(gameList);
                        }
                    }
                }

                // nothing matched - say so and leave the current lists alone, rather than
                // installing an empty result set that leaves nothing to navigate
                if (voiceRecognitionResults.Count == 0)
                {
                    EclipseStateContext.MainWindowViewModel.IsRecognizing = false;

                    DisplayingErrorState noResultsState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
                    noResultsState.ErrorMessage = "No games matched what you said, please try again";
                    EclipseStateContext.TransitionToState(noResultsState);
                    return;
                }

                // remove any prior voice search set and then add these results in the voice search category
                EclipseStateContext.MainWindowViewModel.GameListSets.RemoveAll(set => set.ListCategoryType == ListCategoryType.VoiceSearch);
                EclipseStateContext.MainWindowViewModel.GameListSets.Add(new GameListSet
                {
                    ListCategoryType = ListCategoryType.VoiceSearch,
                    GameLists = voiceRecognitionResults
                                    .OrderByDescending(list => list.MaxMatchPercentage)
                                    .ThenByDescending(list => list.MaxTitleLength)
                                    .ToList()
                });

                // display voice search results
                EclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.VoiceSearch);
                EclipseStateContext.MainWindowViewModel.IsRecognizing = false;
                EclipseStateContext.TransitionToState(EclipseStateContext.GetState(typeof(SelectingGameState)));
            }
            catch(Exception ex)
            {
                EclipseStateContext.MainWindowViewModel.IsRecognizing = false;
                DisplayingErrorState displayingErrorState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
                displayingErrorState.ErrorMessage = ex.Message;
                EclipseStateContext.TransitionToState(displayingErrorState);
                return;
            }
        }
    }
}
