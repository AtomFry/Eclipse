using Eclipse.Service;
using Eclipse.View;
using System;
using System.Timers;

namespace Eclipse.State
{
    public class AttractModeState : EclipseState
    {
        private readonly AttractModeService attractModeService;
        public MainWindowViewModel MainWindowViewModel { get; set; }
        public  MainWindowView MainWindowView { get; set; }
        public EclipseState PreviousState { get; set; }

        private readonly Timer attractModeImageFadeInDelay;
        private readonly Timer attractModeChangeDelay;
        private readonly Timer attractModeLogoFadeInDelay;

        public AttractModeState()
        {
            attractModeService = AttractModeService.Instance;

            // create a timer to delay before fading in an image (2 seconds?)
            attractModeImageFadeInDelay = new Timer(4 * 1000);
            attractModeImageFadeInDelay.Elapsed += AttractModeImageFadeInDelay_Elapsed;
            attractModeImageFadeInDelay.AutoReset = false;

            // create a timer to delay between switching games in attract mode (15 seconds)
            attractModeChangeDelay = new Timer(15 * 1000);
            attractModeChangeDelay.Elapsed += AttractModeChangeDelay_Elapsed;
            attractModeChangeDelay.AutoReset = false;

            // create a timer to delay before fading in the game logo in attract mode (3 seconds)
            attractModeLogoFadeInDelay = new Timer(4 * 1000);
            attractModeLogoFadeInDelay.Elapsed += AttractModeLogoFadeInDelay_Elapsed;
            attractModeLogoFadeInDelay.AutoReset = false;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            // fade the grid in if it isn't already
            MainWindowView.AttractModeFadeToBlack();

            // start timer to delay until the next image will fade in 
            attractModeImageFadeInDelay.Start();
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

        // indicates whether to slide attract mode image left or right 
        private bool attractModeSlideLeft = true;

        // after delay (about 1 second) - fade in the attract mode background image
        private void AttractModeImageFadeInDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            // flip the variable to slide the other way next time
            attractModeSlideLeft = !attractModeSlideLeft;

            // get next attract mode game 
            MainWindowViewModel.NextAttractModeGame();

            // call UI to fade in and slide background
            MainWindowView.AttractModeFadeInAndSlideBackground(attractModeSlideLeft);

            // start timer before we fade in the attract mode clear logo
            attractModeLogoFadeInDelay.Start();

            // start the delay between attract mode games 
            attractModeChangeDelay.Start();
        }

        private void AttractModeLogoFadeInDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            MainWindowView.AttractModeFadeInLogo();
        }

        // when the AttractModeChangeDelay elapses, change games and continue attract mode
        private void AttractModeChangeDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            // fade out this image 
            MainWindowView.AttractModeFadeOutBackgroundAndLogo();

            // start timer to delay until the next image will fade in
            attractModeImageFadeInDelay.Start();
        }

        private void TransitionToPreviousState(EclipseStateContext eclipseStateContext)
        {
            attractModeImageFadeInDelay?.Stop();
            attractModeChangeDelay?.Stop();
            attractModeLogoFadeInDelay?.Stop();

            attractModeService.RestartAttractMode();
            eclipseStateContext.TransitionToState(PreviousState);
        }
    }
}
