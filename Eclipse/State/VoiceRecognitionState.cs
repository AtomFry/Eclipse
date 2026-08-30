using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Helpers;
using Eclipse.Service;
using Eclipse.Models;

namespace Eclipse.State
{
    public class VoiceRecognitionState : EclipseState
    {
        // Offered wherever pressing something again is worth doing. Deliberately absent from the
        // messages where it is not - a recogniser that could not be created will not be created
        // by pressing a button.
        private const string TryAgainHint = "Press any button to try again";

        private ISpeechSessionSource sessionSource;

        // The attempt currently listening, or null. This state object is cached and reused, so
        // the field outlives a search - it is cleared on every way out so that a later Escape
        // cannot cancel a session that has already finished.
        private ISpeechSession speechSession;

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

            sessionSource = SpeechRecognizerService.Instance.GetSessionSource();

            DoRecognize();
        }

        private void ShowUnavailable(VoiceSearchAvailability availability)
        {
            switch (availability)
            {
                case VoiceSearchAvailability.Preparing:
                    Fail("Voice search is still getting ready", "Try again in a moment");
                    break;

                case VoiceSearchAvailability.Failed:
                    // No hint: this one is not going to come right on a retry, and the service
                    // only attempts it once. The reason is in the log.
                    Fail(SpeechRecognizerService.Instance.FailureMessage ?? "Voice search is not available");
                    break;

                default:
                    Fail("Voice search is turned off in the Eclipse settings");
                    break;
            }
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
            // Cancelling gives up on the result as well as stopping the microphone, so the
            // transition below is the only one that happens. It used to be followed by a second
            // one from the completion the cancel raised, which - if the user had already moved
            // on - dragged them back out of wherever they had got to.
            speechSession?.Cancel();
            speechSession = null;

            ReturnToBrowsing();
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
            if (sessionSource == null)
            {
                ReturnToBrowsing();
                return;
            }

            try
            {
                attractModeService.StopAttractMode();

                // stop any video or animations
                EclipseStateContext.MainWindowViewModel.NotifyPresentationInterrupted();

                EclipseStateContext.MainWindowViewModel.EnterVoiceSearch();

                speechSession = sessionSource.StartSession(
                    EclipseStateContext.MainWindowViewModel.UiDispatcher,
                    RecognizeCompleted,
                    PhraseHeard);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "start voice recognition");
                Fail("Voice search could not start, please try again", TryAgainHint);
            }
        }

        // The recogniser's running guess, echoed on screen so the user can see what is being
        // heard while they are still speaking - and so a search that finds nothing is not the
        // first they know about a misheard phrase.
        private void PhraseHeard(string phrase)
        {
            EclipseStateContext.MainWindowViewModel.HeardPhrase = phrase;
        }

        public void RecognizeCompleted(SpeechRecognizerResult speechRecognizerResult)
        {
            // The session is finished with. Raised on the UI thread by the session itself, so
            // everything below is ordinary single-threaded work like any other state transition.
            speechSession = null;

            attractModeService.RestartAttractMode();

            switch (speechRecognizerResult.Outcome)
            {
                // A cancelled recognition is not a search that found nothing - go back to
                // browsing and leave the current lists exactly as they were.
                case SpeechRecognitionOutcome.Cancelled:
                    ReturnToBrowsing();
                    return;

                // Saying nothing is not an error, and it used to be met with a screenful of
                // black. It is the same kind of outcome as matching nothing - the search did not
                // work out, the lists are untouched, and speaking again is the whole recovery -
                // so it is said in the same place, the same way.
                case SpeechRecognitionOutcome.HeardNothing:
                    ShowNotice("Didn't hear anything");
                    return;

                // A real failure still takes the screen. Trying again will not help, so the user
                // should not be left to discover that by trying.
                case SpeechRecognitionOutcome.Failed:
                    Fail("Something went wrong with voice search, please try again", TryAgainHint);
                    return;
            }

            try
            {
                // matching, scoring and ranking are all in the builder - see the rules it names
                List<GameList> voiceRecognitionResults =
                    VoiceSearchResultBuilder.Build(speechRecognizerResult?.RecognizedPhrases,
                                                   VoiceSearchIndex.Instance);

                // nothing matched - say so, and leave the lists alone
                if (voiceRecognitionResults.Count == 0)
                {
                    ShowNotice(NoMatchMessage(speechRecognizerResult));
                    return;
                }

                // replaces any prior voice search set
                EclipseStateContext.MainWindowViewModel.Navigator.InstallSet(new GameListSet
                {
                    ListCategoryType = ListCategoryType.VoiceSearch,
                    GameLists = voiceRecognitionResults
                });

                // display voice search results
                EclipseStateContext.MainWindowViewModel.Navigator.ShowCategory(ListCategoryType.VoiceSearch);
                ReturnToBrowsing();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "build voice search results");
                Fail("Something went wrong with voice search, please try again", TryAgainHint);
            }
        }

        // Tell the user what was heard. "No games matched what you said" gave them nothing to
        // correct against - if it heard "super mary" they had no way of finding that out.
        private static string NoMatchMessage(SpeechRecognizerResult speechRecognizerResult)
        {
            string heard = speechRecognizerResult?.RecognizedPhrases?
                .OrderByDescending(recognizedPhrase => recognizedPhrase.Confidence)
                .ThenByDescending(recognizedPhrase => recognizedPhrase.Phrase?.Length ?? 0)
                .FirstOrDefault()?.Phrase;

            if (string.IsNullOrWhiteSpace(heard))
            {
                return "No games matched what you said";
            }

            return $"Heard “{heard}” - no games matched";
        }

        // Every way out of voice search goes through one of these three, so the screen cannot be
        // left half in it. The listening flag used to be reset by hand on each of the nine exit
        // paths, and none of them put back the overlay the search had covered up.
        //
        // The three differ in how much they interrupt, and that is the whole distinction:
        // ReturnToBrowsing says nothing, ShowNotice says something without taking the screen, and
        // Fail takes the screen. Only a failure the user cannot do anything about gets the last.

        private void ReturnToBrowsing()
        {
            EclipseStateContext.MainWindowViewModel.LeaveVoiceSearch();
            EclipseStateContext.TransitionToState(EclipseStateContext.GetState(typeof(SelectingGameState)));
        }

        /// <summary>
        /// A search that did not work out, said where the list heading goes rather than on a
        /// screen of its own. The lists are untouched and input is live the moment this returns,
        /// so there is nothing to dismiss - the user browses on, or speaks again.
        /// </summary>
        private void ShowNotice(string notice)
        {
            EclipseStateContext.MainWindowViewModel.ShowVoiceSearchNotice(notice);
            EclipseStateContext.TransitionToState(EclipseStateContext.GetState(typeof(SelectingGameState)));
        }

        private void Fail(string message, string hint = null)
        {
            EclipseStateContext.MainWindowViewModel.LeaveVoiceSearch();

            DisplayingErrorState displayingErrorState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
            displayingErrorState.ErrorMessage = message;
            displayingErrorState.ErrorHint = hint;

            EclipseStateContext.TransitionToState(displayingErrorState);
        }
    }
}
