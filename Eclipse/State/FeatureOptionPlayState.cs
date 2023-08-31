using Eclipse.Models;
using Eclipse.Service;
using Eclipse.State.KeyStrategy;
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
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
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
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            KeyStrategyCache.Instance.PageDownStrategy.DoKeyFunction(eclipseStateContext, this);
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            KeyStrategyCache.Instance.PageUpStrategy.DoKeyFunction(eclipseStateContext, this);
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(FeatureOptionMoreInfoState)));
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            if (!held)
            {
                eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
                eclipseStateContext.MainWindowViewModel.CycleListBackward();
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            }
            return true;
        }
    }
}
