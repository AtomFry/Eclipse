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
        Storyboard backgroundImageFadeInSlow_image;
        Storyboard backgroundImageFadeInSlow_black;

        Storyboard backgroundImageFadeOutSlow_image;
        Storyboard backgroundImageFadeOutSlow_black;

        Storyboard backgroundImageFadeOutDelay;
        MainWindowViewModel mainWindowViewModel;

        public MainWindowView()
        {
            InitializeComponent();
            mainWindowViewModel = DataContext as MainWindowViewModel;

            // pass in the animation function that can be called whenever a game changes
            mainWindowViewModel.GameChangeFunction = DoAnimateGameChange;

            // pass in the function that can be called whenever voice recognition loads games and images need to be loaded
            mainWindowViewModel.LoadImagesFunction = SetupGameImage;

            // get a handle on the background fade animations
            backgroundImageFadeOutSlow_black = FindResource("BackgroundImageFadeOutSlow") as Storyboard;
            backgroundImageFadeOutSlow_image = FindResource("BackgroundImageFadeOutSlow") as Storyboard;

            backgroundImageFadeInSlow_black = FindResource("BackgroundImageFadeInSlow") as Storyboard;
            backgroundImageFadeInSlow_image = FindResource("BackgroundImageFadeInSlow") as Storyboard;

            backgroundImageFadeOutDelay = FindResource("BackgroundImageFadeOutDelay") as Storyboard;

            // setting up images async
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new SetupGameImageDelegate(this.SetupGameImage));
        }

        public delegate void SetupGameImageDelegate();
        
        public void SetupGameImage()
        {
            GameList gameListToProcess = null;

            // keep looping until the view model has been initialize
            if (mainWindowViewModel == null || mainWindowViewModel.IsInitializing) 
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new SetupGameImageDelegate(this.SetupGameImage));
                return;
            }

            // process current game list first 
            if ((mainWindowViewModel.CurrentGameList != null) 
                && (mainWindowViewModel.CurrentGameList.MoreImagesToLoad))
            {
                gameListToProcess = mainWindowViewModel.CurrentGameList;
            }

            // if current game list is done then process the next game list
            if ((gameListToProcess == null)
                && (mainWindowViewModel.NextGameList != null)
                && (mainWindowViewModel.NextGameList.MoreImagesToLoad))
            {
                gameListToProcess = mainWindowViewModel.NextGameList;
            }

            // todo: if current and next game list are processed then process any game list in the current/selected list set that has unprocessed images
            if(gameListToProcess == null)
            {
                List<GameList> currentGameLists = mainWindowViewModel?.CurrentGameListSet?.GameLists;
                if (currentGameLists != null)
                {
                    var query = from gameList in currentGameLists
                                where gameList.MoreImagesToLoad
                                select gameList;

                    gameListToProcess = query?.FirstOrDefault();
                }
            }

            if (gameListToProcess == null)
            {
                List<GameListSet> allGameListSets = mainWindowViewModel?.GameListSets;
                if(allGameListSets != null)
                {
                    foreach(GameListSet gameListSet in allGameListSets)
                    {
                        List<GameList> gameLists = gameListSet.GameLists;
                        if(gameLists != null)
                        {
                            var query = from gameList in gameLists
                                        where gameList.MoreImagesToLoad
                                        select gameList;

                            gameListToProcess = query?.FirstOrDefault();
                            if (gameListToProcess != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if(gameListToProcess != null)
            {
                gameListToProcess.LoadNextGameImage();
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new SetupGameImageDelegate(this.SetupGameImage));
                return;
            }
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
                    backgroundImageFadeInSlow_image.Stop();
                    backgroundImageFadeInSlow_black.Stop();
                    backgroundImageFadeInSlow_image.Begin(Image_Selected_BackgroundImage);
                    backgroundImageFadeInSlow_black.Begin(Image_Selected_Background_Black);
                    // todo: do we need to pause when media ends? 
                    // PauseVideo(Video_SelectedGame);
            }
        }

        private void PauseVideo(MediaElement video)
        {
            if(video != null)
            {
                video.Pause();
            }
        }

        private void PlayVideo(MediaElement video)
        {
            if (video != null)
            {
                video.Play();
            }
        }

        private void DoAnimateGameChange()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (mainWindowViewModel.IsDisplayingFeature)
                    {
                        // todo: animate for feature game
                    }
                    else if(mainWindowViewModel.IsDisplayingResults)
                    {
                        // reset opacity 
                        Image_Selected_BackgroundImage.Opacity = 1;
                        Image_Selected_Background_Black.Opacity = 1;

                        // stop animations
                        backgroundImageFadeOutDelay.Stop();
                        backgroundImageFadeInSlow_image.Stop();
                        backgroundImageFadeInSlow_black.Stop();
                        backgroundImageFadeOutSlow_image.Stop();
                        backgroundImageFadeOutSlow_black.Stop();

                        if (Video_SelectedGame != null)
                        {
                            PauseVideo(Video_SelectedGame);

                            if(backgroundImageFadeOutDelay != null)
                            {
                                backgroundImageFadeOutDelay.Begin(Image_Selected_BackgroundImage);
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

        private void BackgroundImageFadeOutDelay_Completed(object sender, EventArgs e)
        {
            PlayVideo(Video_SelectedGame);

            backgroundImageFadeOutSlow_image.Stop();
            backgroundImageFadeOutSlow_black.Stop();
            backgroundImageFadeOutSlow_image.Begin(Image_Selected_BackgroundImage);
            backgroundImageFadeOutSlow_black.Begin(Image_Selected_Background_Black);
        }
    }
}
