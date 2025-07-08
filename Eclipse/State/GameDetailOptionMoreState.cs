using Eclipse.Models;
using Eclipse.Service;
using Eclipse.State.KeyStrategy;
using System;

namespace Eclipse.State
{
    public class GameDetailOptionMoreState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public GameDetailOptionMoreState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingFeature = true;
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingMoreInfo = true;
            eclipseStateContext.MainWindowViewModel.GameDetailOption = GameDetailOption.MoreLikeThis;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionRatingState)));
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingMoreInfo = false;
            eclipseStateContext.MainWindowViewModel.GameListManagement.DoMoreLikeCurrentGame();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.GameListManagement.CheckResetGameLists();

            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingMoreInfo = false;
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
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionFavoriteState)));
            return true;
        }
    }
}
