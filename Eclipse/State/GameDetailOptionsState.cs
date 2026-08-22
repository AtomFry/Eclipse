using Eclipse.Service;
using Eclipse.State.GameDetailOptions;
using Eclipse.State.KeyStrategy;

namespace Eclipse.State
{
    // The game detail overlay: Play, Favorite, More like this, Rating.
    //
    // Replaces four near-identical state classes. Everything they shared - the Escape handler,
    // the Page Up/Down handlers, the attract mode call in every method, and the navigation
    // order - lives here once. What differed between them lives in the option classes, and the
    // order is now just the order of the option list.
    public class GameDetailOptionsState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public GameDetailOptionsState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = true;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = true;

            // This state is cached and reused, so the overlay would otherwise reopen on
            // whichever option it was last left on. It has always opened on the first.
            eclipseStateContext.MainWindowViewModel.GameDetailOptions.Reset();
            eclipseStateContext.MainWindowViewModel.GameDetailOptions.SelectCurrent(eclipseStateContext);
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.GameDetailOptions.CycleForward(eclipseStateContext);
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.GameDetailOptions.CycleBackward(eclipseStateContext);
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            // no attract mode call here - the option makes it, because launching a game stops
            // the screen saver outright while the other options merely restart the idle timer
            eclipseStateContext.MainWindowViewModel.GameDetailOptions.SelectedOption.Activate(eclipseStateContext);
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            // Escape abandons rather than commits - only the rating option acts on it
            eclipseStateContext.MainWindowViewModel.GameDetailOptions.SelectedOption.OnCancelled(eclipseStateContext);

            eclipseStateContext.MainWindowViewModel.CheckResetGameLists();

            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.GameDetailOptions.SelectedOption.MoveLeft(eclipseStateContext);
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.GameDetailOptions.SelectedOption.MoveRight(eclipseStateContext);
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
    }
}
