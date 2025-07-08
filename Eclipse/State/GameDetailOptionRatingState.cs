using Eclipse.Models;
using Eclipse.Service;
using Eclipse.State.KeyStrategy;
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
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingFeature = true;
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingMoreInfo = true;
            eclipseStateContext.MainWindowViewModel.GameDetailOption = GameDetailOption.Rating;
            eclipseStateContext.MainWindowViewModel.UIState.IsRatingGame = true;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.UIState.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.GameOperations.SaveRatingCurrentGame();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionPlayState)));
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.GameOperations.SaveRatingCurrentGame();
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.GameListManagement.CheckResetGameLists();

            eclipseStateContext.MainWindowViewModel.UIState.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingMoreInfo = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.GameOperations.RateCurrentGame(-0.5f);
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            // do not perform page up/down while rating game - it's not clear whether it should save or cancel the rating change

            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            // do not perform page up/down while rating game - it's not clear whether it should save or cancel the rating change

            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.GameOperations.RateCurrentGame(0.5f);
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.UIState.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.GameOperations.SaveRatingCurrentGame();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionMoreState)));
            return true;
        }
    }
}
