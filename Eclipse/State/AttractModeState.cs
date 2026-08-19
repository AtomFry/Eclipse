using Eclipse.Service;

namespace Eclipse.State
{
    // Drives the screen saver. The sequence lives in AttractModeSlideshow and the service owns
    // the running one, so this state is now only "start it on entry, and let any input end it".
    public class AttractModeState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        /// <summary>
        /// Where to go back to on any input. Attract mode returns the user to exactly where
        /// they were, not to a default state (RULE-ATTRACT-008).
        /// </summary>
        public EclipseState PreviousState { get; set; }

        public AttractModeState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.StartSlideshow();
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
            // RestartAttractMode stops the slideshow, turns the visuals off and re-arms the
            // idle timer
            attractModeService.RestartAttractMode();
            eclipseStateContext.TransitionToState(PreviousState);
        }
    }
}
