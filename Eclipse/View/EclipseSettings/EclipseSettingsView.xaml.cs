using Eclipse.Event;
using Eclipse.Helpers;
using Prism.Events;
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
        readonly EventAggregator eventAggregator;

        public EclipseSettingsView()
        {
            InitializeComponent();

            eventAggregator = EventAggregatorHelper.Instance.EventAggregator;
            eventAggregator.GetEvent<EclipseSettingsClose>().Subscribe(OnEclipseSettingsClose);

            eclipseSettingsViewModel = new EclipseSettingsViewModel();
            DataContext = eclipseSettingsViewModel;
            Loaded += EclipseSettingsView_Loaded;
            PreviewKeyDown += EclipseSettingsView_PreviewKeyDown;
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

        private async void EclipseSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            await eclipseSettingsViewModel.LoadAsync();
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
