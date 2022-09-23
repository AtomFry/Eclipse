using Eclipse.Models;
using Eclipse.Service;
using System;

namespace Eclipse.State
{
    public class FeatureOptionPlayState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public FeatureOptionPlayState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.FeatureOption = FeatureGameOption.PlayGame;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = true;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.TransitionToState(new SelectingGameState());
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.StopAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.PlayCurrentGame();
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.TransitionToState(new SelectingGameState());
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.TransitionToState(new SelectingOptionsState());
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.TransitionToState(new VoiceRecognitionState());
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.DoRandomGame();
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.TransitionToState(new FeatureOptionMoreInfoState());
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            if (!held)
            {
                eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
                eclipseStateContext.MainWindowViewModel.CycleListBackward();
                eclipseStateContext.TransitionToState(new SelectingGameState());
            }
            return true;
        }
    }
}
