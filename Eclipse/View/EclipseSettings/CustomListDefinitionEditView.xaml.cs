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
    /// Interaction logic for CustomListDefinitionEditView.xaml
    /// </summary>
    public partial class CustomListDefinitionEditView : Window
    {

        public CustomListDefinitionEditView(CustomListDefinitionEditViewModel customListDefinitionEditViewModel)
        {
            InitializeComponent();

            SettingsEvents.CustomListDefinitionEditClose += OnPatcherEditClose;

            DataContext = customListDefinitionEditViewModel;

            Closing += CustomListDefinitionEditView_Closing;
            PreviewKeyDown += CustomListDefinitionEditView_PreviewKeyDown;
            Closed += CustomListDefinitionEditView_Closed;
        }

        // Everything the constructor attached, detached. The event aggregator is a
        // process-lifetime singleton and this window is opened and closed repeatedly, so a
        // subscription left behind means a closed window still receiving events for as long as
        // it happens to stay alive.
        private void CustomListDefinitionEditView_Closed(object sender, EventArgs e)
        {
            SettingsEvents.CustomListDefinitionEditClose -= OnPatcherEditClose;

            Closing -= CustomListDefinitionEditView_Closing;
            PreviewKeyDown -= CustomListDefinitionEditView_PreviewKeyDown;
            Closed -= CustomListDefinitionEditView_Closed;
        }

        private void CustomListDefinitionEditView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void CustomListDefinitionEditView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SettingsEvents.RaiseCustomListDefinitionEditClosing();
        }

        private void OnPatcherEditClose()
        {
            SettingsEvents.RaiseCustomListDefinitionEditClosing();
            Close();
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