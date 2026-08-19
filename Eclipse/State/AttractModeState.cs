using Eclipse.Service;
using Eclipse.View;

namespace Eclipse.State
{
    // Drives the screen saver. The sequence itself lives in AttractModeSlideshow; this state
    // starts it on entry and cancels it on any input.
    public class AttractModeState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public MainWindowViewModel MainWindowViewModel { get; set; }
        public IAttractModePresenter Presenter { get; set; }
        public EclipseState PreviousState { get; set; }

        private AttractModeSlideshow slideshow;

        public AttractModeState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            // Built per entry rather than in the constructor: this state is cached and reused,
            // and a slideshow owns a cancellation token for exactly one run. The three timers
            // that used to live here were created once and never disposed.
            slideshow?.Stop();

            slideshow = new AttractModeSlideshow(
                Presenter,
                AttractModeTimings.Current,
                () => MainWindowViewModel?.IsPlayingGame != true);

            slideshow.Start();
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            TransitionToPreviousState(eclipseStateContext);
            return true;
        }

        private void TransitionToPreviousState(EclipseStateContext eclipseStateContext)
        {
            slideshow?.Stop();

            attractModeService.RestartAttractMode();
            eclipseStateContext.TransitionToState(PreviousState);
        }
    }
}
