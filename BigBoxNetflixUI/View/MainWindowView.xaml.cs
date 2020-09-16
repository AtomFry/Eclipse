using BigBoxNetflixUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
        private Timer backgroundImageChangeDelay;
        private Timer fadeOutForMovieDelay;

        private BitmapImage activeBackgroundImage;

        private Storyboard BackgroundImageFadeInSlowStoryBoard;
        private Storyboard BackgroundImageFadeOutSlowStoryBoard;
        private Storyboard BackgroundImageFadeInAfterVideoStoryBoard;


        public MainWindowView()
        {
            InitializeComponent();

            // create a timer to delay swapping background images
            backgroundImageChangeDelay = new Timer(1000);
            backgroundImageChangeDelay.Elapsed += BackgroundImageChangeDelay_Elapsed;
            backgroundImageChangeDelay.AutoReset = false;

            // create a timer to delay playing movie and fading out background images
            fadeOutForMovieDelay = new Timer(2000);
            fadeOutForMovieDelay.Elapsed += FadeOutForMovieDelay_Elapsed;
            fadeOutForMovieDelay.AutoReset = false;

            BackgroundImageFadeInSlowStoryBoard = FindResource("BackgroundImageFadeInSlow") as Storyboard;
            BackgroundImageFadeOutSlowStoryBoard = FindResource("BackgroundImageFadeOutSlow") as Storyboard;
            BackgroundImageFadeInAfterVideoStoryBoard = FindResource("BackgroundImageFadeInAfterVideo") as Storyboard;

            // get handle on the view model 
            mainWindowViewModel = DataContext as MainWindowViewModel;

            // pass in the animation function that can be called whenever a game changes
            mainWindowViewModel.GameChangeFunction = DoAnimateGameChange;

            // pass in the function that can be called whenever voice recognition loads games and images need to be loaded
            mainWindowViewModel.LoadImagesFunction = SetupGameImage;

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

        private void DimBackground()
        {
            if (Image_Displayed_BackgroundImage.Opacity != 0.25)
            {
                DoubleAnimation dimmingDisplayedBackground = new DoubleAnimation(Image_Displayed_BackgroundImage.Opacity, 0.25, TimeSpan.FromMilliseconds(25));
                Image_Displayed_BackgroundImage.BeginAnimation(Image.OpacityProperty, dimmingDisplayedBackground);
            }

            if (Image_Selected_Background_Black.Opacity != 1)
            {
                DoubleAnimation dimmingBlackBackground = new DoubleAnimation(Image_Selected_Background_Black.Opacity, 1.00, TimeSpan.FromMilliseconds(25));
                Image_Selected_Background_Black.BeginAnimation(Image.OpacityProperty, dimmingBlackBackground);
            }

            if (Image_Active_BackgroundImage.Opacity != 0)
            {
                DoubleAnimation dimmingActiveBackground = new DoubleAnimation(Image_Active_BackgroundImage.Opacity, 0, TimeSpan.FromMilliseconds(25));
                Image_Active_BackgroundImage.BeginAnimation(Image.OpacityProperty, dimmingActiveBackground);
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
                        // dim background image
                        DimBackground();

                        // pause the video
                        PauseVideo(Video_SelectedGame);
                        
                        // stop timers
                        fadeOutForMovieDelay.Stop();
                        backgroundImageChangeDelay.Stop();

                        // update the active background image
                        activeBackgroundImage = null;
                        Uri uri = mainWindowViewModel?.CurrentGameList?.Game1?.BackgroundImage;
                        if (uri != null)
                        {
                            activeBackgroundImage = new BitmapImage(uri);
                        }

                        // start the timer - when it goes off, fade in the new background image
                        backgroundImageChangeDelay.Start();
                    }
                }
                catch (Exception ex)
                {
                    Helpers.LogException(ex, "MainWindowView.xaml.cs.DoAnimateGameChange");
                }
            });
        }

        // delay for an interval when selecting games and then fade in the current selected game
        private void BackgroundImageChangeDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Image_Active_BackgroundImage != null)
                    {
                        Image_Active_BackgroundImage.Opacity = 0;
                        Image_Active_BackgroundImage.Source = activeBackgroundImage;
                    }

                    // fade in the active background image 
                    BackgroundImageFadeInSlowStoryBoard.Begin(Image_Active_BackgroundImage);
                }
                catch (Exception ex)
                {
                    Helpers.LogException(ex, "BackgroundImageChangeDelay_Elapsed");
                }
            });
        }

        private void BackgroundImageFadeInSlow_Completed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // swap displayed image to current active image
                    Image_Displayed_BackgroundImage.Source = Image_Active_BackgroundImage.Source;

                    // hide the active image
                    Image_Active_BackgroundImage.Opacity = 0;

                    // start timer to delay to fade out image and play video
                    fadeOutForMovieDelay.Start();
                }
                catch (Exception ex)
                {
                    Helpers.LogException(ex, "BackgroundImageFadeInSlow_Completed");
                }
            });
        }

        private void FadeOutForMovieDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (Video_SelectedGame != null)
                    {
                        PlayVideo(Video_SelectedGame);
                        BackgroundImageFadeOutSlowStoryBoard.Begin(Image_Displayed_BackgroundImage);
                        BackgroundImageFadeOutSlowStoryBoard.Begin(Image_Active_BackgroundImage);
                        BackgroundImageFadeOutSlowStoryBoard.Begin(Image_Selected_Background_Black);
                    }
                });
            }
            catch(Exception ex)
            {
                Helpers.LogException(ex, "FadeOutForMovieDelay_Elapsed");
            }
        }

        private void Video_SelectedGame_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    BackgroundImageFadeInAfterVideoStoryBoard.Begin(Image_Selected_Background_Black);
                    BackgroundImageFadeInAfterVideoStoryBoard.Begin(Image_Displayed_BackgroundImage);
                });
            }
            catch(Exception ex)
            {
                Helpers.LogException(ex, "Video_SelectedGame_MediaEnded");
            }
        }
    }
}
