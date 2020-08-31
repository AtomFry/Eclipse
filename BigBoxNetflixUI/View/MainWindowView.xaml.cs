using BigBoxNetflixUI.Models;
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
        Storyboard imageFadeIn;
        Storyboard imageFadeOut;
        MainWindowViewModel mainWindowViewModel;

        public MainWindowView()
        {
            InitializeComponent();
            mainWindowViewModel = DataContext as MainWindowViewModel;

            // pass in the animation function that can be called whenever a game changes
            mainWindowViewModel.GameChangeFunction = DoAnimateGameChange;

            // get a handle on the background fade animations
            imageFadeOut = FindResource("BackgroundImageFadeOut") as Storyboard;
            imageFadeIn = FindResource("BackgroundImageFadeIn") as Storyboard;


            // todo: try setting up images async
            Helpers.Log("MainWindowViewConstructor");
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new SetupGameImageDelegate(this.SetupGameImage));
        }

        // todo: test variables for testing async image loading
        public delegate void SetupGameImageDelegate();
        GameList currentGameList;
        bool startedLoading = false;
        
        public void SetupGameImage()
        {
            currentGameList = null;
            if (mainWindowViewModel == null)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new SetupGameImageDelegate(this.SetupGameImage));
            }

            // process current game list first 
            if (mainWindowViewModel.CurrentGameList != null)
            {
                if (mainWindowViewModel.CurrentGameList.MoreImagesToLoad)
                {
                    currentGameList = mainWindowViewModel.CurrentGameList;
                }
            }

            // if current game list is done then process the next game list
            if (currentGameList == null)
            {
                if(mainWindowViewModel.NextGameList != null)
                {
                    if (mainWindowViewModel.NextGameList.MoreImagesToLoad)
                    {
                        currentGameList = mainWindowViewModel.NextGameList;
                    }
                }
            }

            // if current and next game list are processed then process any game list that has unloaded images
            if (currentGameList == null)
            {
                if (mainWindowViewModel.PlatformGameLists != null)
                {
                    var gameListQuery = from list in mainWindowViewModel.PlatformGameLists
                                        where list.MoreImagesToLoad
                                        select list;

                    currentGameList = gameListQuery.FirstOrDefault();
                }
            }


            if(currentGameList != null)
            {
                currentGameList.LoadNextGameImage();
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new SetupGameImageDelegate(this.SetupGameImage));
                startedLoading = true;
                return;
            }

            if(!startedLoading)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new SetupGameImageDelegate(this.SetupGameImage));
            }

            /*
            if (mainWindowViewModel.VoiceSearchGameLists != null)
            {

            }

            if(mainWindowViewModel.GenreGameLists != null)
            {

            }

            if(mainWindowViewModel.SeriesGameLists != null)
            {

            }

            if(mainWindowViewModel.ReleaseYearGameLists != null)
            {

            }

            if(mainWindowViewModel.PlaylistGameLists != null)
            {

            }

            if(mainWindowViewModel.PlayModeGameLists != null)
            {

            }
            */
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
                if(imageFadeIn != null)
                {
                    imageFadeIn.Begin(Image_Selected_BackgroundImage);
                    imageFadeIn.Begin(Image_Selected_Background_Black);
                }
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

        private void DoAnimateGameChange()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // todo: only fade background images out if there is a video 
                    // fade in background image
                    // start fade out only if Video_SelectedGame.Source is not null
                    imageFadeIn.Begin(Image_Selected_BackgroundImage);
                    imageFadeIn.Begin(Image_Selected_Background_Black);

                    // pause the selected game video
                    if (Video_SelectedGame != null)
                    {
                        Video_SelectedGame.Pause();

                        if (Video_SelectedGame.Source != null)
                        {
                            // fade the background images
                            if (imageFadeOut != null)
                            {
                                imageFadeOut.Begin(Image_Selected_BackgroundImage);
                                imageFadeOut.Begin(Image_Selected_Background_Black);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helpers.LogException(ex, "MainWindowView.xaml.cs.DoAnimateGameChange");
                }
            });
        }
    }
}
