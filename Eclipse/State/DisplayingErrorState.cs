using Eclipse.Converters;
using Eclipse.Service;

namespace Eclipse.State
{
    public class DisplayingErrorState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public string ErrorMessage { get; set; }

        // What the user can do about it, or null where there is nothing to do. Set alongside
        // ErrorMessage by callers that have a recovery to offer - "heard nothing" and "matched
        // nothing" do, "voice search is not available on this PC" does not.
        public string ErrorHint { get; set; }

        public DisplayingErrorState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.ErrorMessage = ErrorMessage;
            eclipseStateContext.MainWindowViewModel.ErrorHint = ErrorHint;
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = true;

            // This state is cached and reused, so anything left here would be shown again by the
            // next caller that does not set it - and most callers only set the message.
            ErrorMessage = null;
            ErrorHint = null;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsDisplayingError = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }
    }
}
