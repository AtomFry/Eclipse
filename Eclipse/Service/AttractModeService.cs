using Eclipse.State;
using Eclipse.View;
using System.Timers;

namespace Eclipse.Service
{
    public sealed class AttractModeService
    {
        public IAttractModePresenter Presenter { get; set; }
        public MainWindowViewModel MainWindowViewModel { get; set; }

        private readonly Timer attractModeDelay;

        private AttractModeSlideshow slideshow;

        public static AttractModeService Instance => instance;

        private static readonly AttractModeService instance = new AttractModeService();

        static AttractModeService()
        {
        }

        private AttractModeService()
        {
            if (EclipseSettingsDataProvider.Instance.EclipseSettings.EnableScreenSaver)
            {
                // create a timer to delay for attract mode
                attractModeDelay = new Timer(EclipseSettingsDataProvider.Instance.EclipseSettings.ScreensaverDelayInSeconds * 1000);
                attractModeDelay.Elapsed += AttractModeDelay_Elapsed;
                attractModeDelay.AutoReset = false;
            }
        }

        // when the AttractModeDelay elapses, start into attract mode 
        private void AttractModeDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (MainWindowViewModel.IsPlayingGame) return;

            AttractModeState attractModeState = MainWindowViewModel.EclipseStateContext.GetState(typeof(AttractModeState)) as AttractModeState;

            // the state only needs to know where to return to - the service owns the
            // presenter and the slideshow
            attractModeState.PreviousState = MainWindowViewModel.EclipseStateContext.CurrentState;

            MainWindowViewModel.EclipseStateContext.TransitionToState(attractModeState);
        }

        /// <summary>
        /// Starts the slideshow for a run of attract mode. Built per entry rather than kept
        /// around, because a slideshow owns a cancellation token for exactly one run. The
        /// timings are taken per run too - not to pick up settings changes, which need a
        /// restart, but so the slideshow and the view can never disagree about a value.
        /// </summary>
        public void StartSlideshow()
        {
            StopSlideshow();

            slideshow = new AttractModeSlideshow(
                Presenter,
                AttractModeTimings.Current,
                () => MainWindowViewModel?.NextAttractModeGame(),
                () => MainWindowViewModel?.IsPlayingGame != true);

            slideshow.Start();
        }

        private void StopSlideshow()
        {
            AttractModeSlideshow running = slideshow;
            slideshow = null;
            running?.Stop();
        }

        public void StopAttractMode()
        {
            attractModeDelay?.Stop();

            // The slideshow used to be owned by AttractModeState, which stopped it only on
            // the way out through an input handler. Stopping the screen saver by any other
            // route - a game launching, a video preview starting - turned the visuals off and
            // left the loop cycling invisibly until the next entry replaced it.
            StopSlideshow();

            if (Presenter != null)
            {
                Presenter.TurnOff();
            }
        }

        public void RestartAttractMode()
        {
            StopAttractMode();
            attractModeDelay?.Start();
        }
    }
}