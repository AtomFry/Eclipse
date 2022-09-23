using Eclipse.State;
using Eclipse.View;
using System.Security.RightsManagement;
using System.Timers;
using System.Windows.Threading;

namespace Eclipse.Service
{
    public delegate void AttractModeTurnOff();
    public delegate void AttractModeFadeToBlack();
    public delegate void AttractModeFadeInAndSlideBackground(bool slideLeft);
    public delegate void AttractModeFadeInLogo();
    public delegate void AttractModeFadeOutBackgroundAndLogo();

    public sealed class AttractModeService
    {
        public MainWindowView MainWindowView { get; set; }
        public MainWindowViewModel MainWindowViewModel { get; set; }

        private readonly Timer attractModeDelay;

        public static AttractModeService Instance => instance;

        private static readonly AttractModeService instance = new AttractModeService();

        static AttractModeService()
        {
        }

        private AttractModeService()
        {
            // create a timer to delay for attract mode (60 seconds)
            attractModeDelay = new Timer(60 * 1000);
            attractModeDelay.Elapsed += AttractModeDelay_Elapsed;
            attractModeDelay.AutoReset = false;
        }

        // when the AttractModeDelay elapses, start into attract mode 
        private void AttractModeDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            MainWindowViewModel.EclipseStateContext.TransitionToState(new AttractModeState(MainWindowViewModel, MainWindowView, MainWindowViewModel.EclipseStateContext.CurrentState));
        }

        public void StopAttractMode()
        {
            attractModeDelay?.Stop();
            MainWindowView.AttractModeTurnOff();
        }

        public void RestartAttractMode()
        {
            StopAttractMode();
            attractModeDelay.Start();
        }
    }
}