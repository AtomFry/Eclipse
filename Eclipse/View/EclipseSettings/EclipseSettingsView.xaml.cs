using Eclipse.Event;
using Eclipse.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Eclipse.View.EclipseSettings
{
    /// <summary>
    /// Interaction logic for EclipseSettingsView.xaml
    /// </summary>
    public partial class EclipseSettingsView : Window
    {
        readonly EclipseSettingsViewModel eclipseSettingsViewModel;

        public EclipseSettingsView()
        {
            InitializeComponent();

            SettingsEvents.EclipseSettingsClose += OnEclipseSettingsClose;

            eclipseSettingsViewModel = new EclipseSettingsViewModel();
            DataContext = eclipseSettingsViewModel;
            Loaded += EclipseSettingsView_Loaded;
            PreviewKeyDown += EclipseSettingsView_PreviewKeyDown;
            Closed += EclipseSettingsView_Closed;
        }

        // Everything the constructor attached, detached - the window's own handlers, its
        // subscription, and the view model's two. The event aggregator is a process-lifetime
        // singleton and this window is opened and closed repeatedly, so a subscription left
        // behind means a closed window still receiving events for as long as it stays alive.
        private void EclipseSettingsView_Closed(object sender, EventArgs e)
        {
            SettingsEvents.EclipseSettingsClose -= OnEclipseSettingsClose;
            eclipseSettingsViewModel.Detach();

            Loaded -= EclipseSettingsView_Loaded;
            PreviewKeyDown -= EclipseSettingsView_PreviewKeyDown;
            Closed -= EclipseSettingsView_Closed;
        }

        private void OnEclipseSettingsClose()
        {
            Close();
        }

        private void EclipseSettingsView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        // An event handler, so void is forced. Without the guard, a settings file that could not
        // be loaded threw onto the dispatcher and the window came up blank with no explanation.
        private async void EclipseSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await eclipseSettingsViewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "load the Eclipse settings window");

                MessageDialogHelper.ShowOKDialog(
                    $"Your settings could not be loaded.\n\n{ex.Message}\n\nClose this window without saving, or saving will overwrite them.",
                    "Load failed");
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Grid)
            {
                WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
