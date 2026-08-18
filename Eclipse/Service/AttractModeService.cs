using Eclipse.State;
using Eclipse.View;
using System.Timers;

namespace Eclipse.Service
{
    public delegate void AttractModeTurnOff();
    public delegate void AttractModeFadeToBlack();
    public delegate void AttractModeFadeInAndSlideBackground(bool slideLeft);
    public delegate void AttractModeFadeInLogo();
    public delegate void AttractModeFadeOutBackgroundAndLogo();

    public sealed class AttractModeService
    {
        public IAttractModePresenter Presenter { get; set; }
        public MainWindowViewModel MainWindowViewModel { get; set; }

        private readonly Timer attractModeDelay;

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

            attractModeState.Presenter = Presenter;
            attractModeState.MainWindowViewModel = MainWindowViewModel;
            attractModeState.PreviousState = MainWindowViewModel.EclipseStateContext.CurrentState;

            MainWindowViewModel.EclipseStateContext.TransitionToState(attractModeState);
        }

        public void StopAttractMode()
        {
            attractModeDelay?.Stop();

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