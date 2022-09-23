using Eclipse.Models;
using Eclipse.Service;
using System;

namespace Eclipse.State
{
    public class GameDetailOptionRatingState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public GameDetailOptionRatingState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = true;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = true;
            eclipseStateContext.MainWindowViewModel.GameDetailOption = GameDetailOption.Rating;
            eclipseStateContext.MainWindowViewModel.IsRatingGame = true;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
            eclipseStateContext.TransitionToState(new GameDetailOptionPlayState());
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.CheckResetGameLists();

            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = false;
            eclipseStateContext.TransitionToState(new SelectingGameState());
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.RateCurrentGame(-0.5f);
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.TransitionToState(new VoiceRecognitionState());
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
            eclipseStateContext.MainWindowViewModel.DoRandomGame();
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.RateCurrentGame(0.5f);
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
            eclipseStateContext.TransitionToState(new GameDetailOptionMoreState());
            return true;
        }
    }
}
