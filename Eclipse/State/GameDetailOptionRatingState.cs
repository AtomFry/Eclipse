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
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = true;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = true;
            eclipseStateContext.MainWindowViewModel.GameDetailOption = GameDetailOption.Rating;
            eclipseStateContext.MainWindowViewModel.IsRatingGame = true;

            // remember the rating on the way in so Escape can put it back
            eclipseStateContext.MainWindowViewModel.BeginRatingCurrentGame();
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionPlayState)));
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

            // Escape means cancel - restore the rating the editor opened with. The value is
            // written into the game as the user moves it, so without this the change stuck
            // even though Eclipse never saved it.
            eclipseStateContext.MainWindowViewModel.CancelRatingCurrentGame();

            eclipseStateContext.MainWindowViewModel.CheckResetGameLists();

            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
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
            eclipseStateContext.MainWindowViewModel.RateCurrentGame(0.5f);
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionMoreState)));
            return true;
        }
    }
}
