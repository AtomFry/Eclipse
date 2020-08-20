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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace BigBoxNetflixUI.View
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : UserControl, IBigBoxThemeElementPlugin
    {
        MainWindowViewModel mainWindowViewModel;

        public MainWindowView()
        {
            InitializeComponent();
            mainWindowViewModel = DataContext as MainWindowViewModel;
        }

        public bool OnDown(bool held)
        {
            mainWindowViewModel.DoDown(held);
            return true;
        }

        public bool OnEnter()
        {
            mainWindowViewModel.DoEnter();
            return true;
        }

        public bool OnEscape()
        {
            mainWindowViewModel.DoEscape();
            return false;
        }

        public bool OnLeft(bool held)
        {
            mainWindowViewModel.DoLeft(held);

            // todo: check conditions for fade out and video pause
            ((Storyboard)FindResource("BackgroundImageFadeOut")).Begin(Image_Selected_BackgroundImage);
            ((Storyboard)FindResource("BackgroundImageFadeOut")).Begin(Image_Selected_Background_Black);
            if (Video_SelectedGame != null) Video_SelectedGame.Pause();

            return true;
        }

        public bool OnPageDown()
        {
            mainWindowViewModel.DoPageDown();
            return true;
        }

        public bool OnPageUp()
        {
            mainWindowViewModel.DoPageUp();
            return true;
        }

        public bool OnRight(bool held)
        {
            mainWindowViewModel.DoRight(held);

            // todo: check conditions for fade out and video pause
            ((Storyboard)FindResource("BackgroundImageFadeOut")).Begin(Image_Selected_BackgroundImage);
            ((Storyboard)FindResource("BackgroundImageFadeOut")).Begin(Image_Selected_Background_Black);
            if (Video_SelectedGame != null) Video_SelectedGame.Pause();

            return true;
        }

        public void OnSelectionChanged(FilterType filterType, string filterValue, IPlatform platform, IPlatformCategory category, IPlaylist playlist, IGame game)
        {
        }

        public bool OnUp(bool held)
        {
            mainWindowViewModel.DoUp(held);
            return true;
        }

        private void Video_SelectedGame_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mainWindowViewModel?.CurrentGameList?.Game1 != null)
            {
                ((Storyboard)FindResource("BackgroundImageFadeIn")).Begin(Image_Selected_BackgroundImage);
                ((Storyboard)FindResource("BackgroundImageFadeIn")).Begin(Image_Selected_Background_Black);
            }
        }

        private void BackgroundImageFadeIn_Completed(object sender, EventArgs e)
        {
            if (Video_SelectedGame != null) Video_SelectedGame.Pause();
        }

        private void BackgroundImageFadeOut_Completed(object sender, EventArgs e)
        {
            if (Video_SelectedGame != null) Video_SelectedGame.Play();
        }
    }
}
