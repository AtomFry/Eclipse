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

            speechRecognizer = SpeechRecognizerService.Instance.GetRecognizer();

            DoRecognize();
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
            speechRecognizer.TryCancelRecognition();
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

            try
            {
                List<GameList> voiceRecognitionResults = new List<GameList>();

                // speech hypothesized adds phrases that it heard to the TempGameLists collection
                // for each phrase, get the set of matching games from the GameTitlePhrases dictionary
                if (speechRecognizerResult?.RecognizedPhrases?.Count() > 0)
                {
                    // in case the same phrase was recognized multiple times, group by phrase and keep only the max confidence
                    List<GameList> distinctGameLists = speechRecognizerResult?.RecognizedPhrases
                        .GroupBy(s => s.Phrase)
                        .Select(s => new GameList { ListDescription = s.Key, Confidence = s.Max(m => m.Confidence) }).ToList();

                    // loop through the gamelists (one list for each hypothesized phrase)
                    foreach (GameList gameList in distinctGameLists)
                    {
                        // get the list of matching games for the phrase from the GameTitlePhrases dictionary 
                        IEnumerable<GameMatch> query = from game in EclipseStateContext.MainWindowViewModel.gameBag
                                                       where game.CategoryType == ListCategoryType.VoiceSearch
                                                       && game.CategoryValue == gameList.ListDescription
                                                       group game by game into grouping
                                                       select GameMatch.CloneGameMatch(grouping.Key, ListCategoryType.VoiceSearch, gameList.ListDescription, grouping.Max(g => g.TitleMatchType), grouping.Key.ConvertedTitle);

                        if (query.Any())
                        {
                            List<GameMatch> matches = query.ToList();

                            foreach (GameMatch game in matches)
                            {
                                game.SetupVoiceMatchPercentage(gameList.Confidence, gameList.ListDescription);
                            }
                            gameList.MatchingGames = matches.OrderByDescending(match => match.MatchPercentage).ToList();
                            voiceRecognitionResults.Add(gameList);
                        }
                    }
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
