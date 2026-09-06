using System;
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

        // Where Escape goes. Almost always back to browsing, but text search opens this overlay
        // too and abandoning it there should return to the search rather than dropping the user
        // out of it - see OnEscape.
        //
        // Two fields rather than one because this state is cached and reused: the opener sets
        // the pending value, EnterState consumes it, and anything that opens the overlay without
        // asking for a destination gets the default. A stale destination therefore cannot
        // survive a single use - the same hazard the Reset() below exists for.
        private Type pendingReturnTo;
        private Type returnTo = typeof(SelectingGameState);

        public GameDetailOptionsState()
        {
            attractModeService = AttractModeService.Instance;
        }

        /// <summary>
        /// Opens the overlay, saying where Escape should go if the user abandons it.
        /// </summary>
        public static void OpenReturningTo(EclipseStateContext eclipseStateContext, Type returnTo)
        {
            GameDetailOptionsState state =
                (GameDetailOptionsState)eclipseStateContext.GetState(typeof(GameDetailOptionsState));

            state.pendingReturnTo = returnTo;
            eclipseStateContext.TransitionToState(state);
        }

        /// <summary>
        /// Re-enters the overlay from a screen the overlay itself opened, keeping the destination
        /// Escape was given.
        ///
        /// Without this, stepping out to the more-info screen and back would quietly reset where
        /// Escape goes - so a user who reached the overlay from a search, looked at more info,
        /// came back and then changed their mind would be dropped out of the search rather than
        /// returned to it.
        /// </summary>
        public static void Reopen(EclipseStateContext eclipseStateContext)
        {
            GameDetailOptionsState state =
                (GameDetailOptionsState)eclipseStateContext.GetState(typeof(GameDetailOptionsState));

            OpenReturningTo(eclipseStateContext, state.returnTo);
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            returnTo = pendingReturnTo ?? typeof(SelectingGameState);
            pendingReturnTo = null;

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

            // Back to whoever opened this, which is browsing unless someone said otherwise.
            // Escape abandons, and abandoning should undo the press that got here rather than
            // landing the user somewhere neither state chose.
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(returnTo));
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
