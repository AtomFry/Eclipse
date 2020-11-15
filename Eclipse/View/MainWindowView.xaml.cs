using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.RightsManagement;
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

namespace Eclipse.View
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : UserControl, IBigBoxThemeElementPlugin
    {
        MainWindowViewModel mainWindowViewModel;
        private Timer backgroundImageChangeDelay;
        private Timer fadeOutForMovieDelay;
        private Timer attractModeDelay;
        private Timer attractModeImageFadeInDelay;
        private Timer attractModeChangeDelay;

        private BitmapImage activeBackgroundImage;
        private BitmapImage activeClearLogo;
        private string activeMatchPercentageText;
        private string activeReleaseYearText;
        private BitmapImage activeCommunityStarRatingImage;
        private BitmapImage activePlayModeImage;
        private BitmapImage activePlatformLogoImage;
        private BitmapImage activeGameBezelImage;
        private BitmapImage activeAttractModeBackgroundImage;
        private BitmapImage activeAttractModeClearLogo;

        private Storyboard BackgroundImageFadeInSlowStoryBoard;

        private LinearGradientBrush opacityBrush = GetOpacityBrush();

        public MainWindowView()
        {
            InitializeComponent();

            SetupStopVideoAndAnimationWorker();

            // create a timer to delay swapping background images
            backgroundImageChangeDelay = new Timer(1000);
            backgroundImageChangeDelay.Elapsed += BackgroundImageChangeDelay_Elapsed;
            backgroundImageChangeDelay.AutoReset = false;

            // create a timer to delay playing movie and fading out background images
            fadeOutForMovieDelay = new Timer(2000);
            fadeOutForMovieDelay.Elapsed += FadeOutForMovieDelay_Elapsed;
            fadeOutForMovieDelay.AutoReset = false;

            // create a timer to delay for attract mode (90 seconds)
            attractModeDelay = new Timer(5 * 1000);
            attractModeDelay.Elapsed += AttractModeDelay_Elapsed;
            attractModeDelay.AutoReset = false;

            // create a timer to delay before fading in an image (2 seconds?)
            attractModeImageFadeInDelay = new Timer(4 * 1000);
            attractModeImageFadeInDelay.Elapsed += AttractModeImageFadeInDelay_Elapsed;
            attractModeImageFadeInDelay.AutoReset = false;

            // create a timer to delay between switching games in attract mode (15 seconds)
            attractModeChangeDelay = new Timer(15 * 1000);
            attractModeChangeDelay.Elapsed += AttractModeChangeDelay_Elapsed;
            attractModeChangeDelay.AutoReset = false;


            BackgroundImageFadeInSlowStoryBoard = FindResource("BackgroundImageFadeInSlow") as Storyboard;

            // get handle on the view model 
            mainWindowViewModel = DataContext as MainWindowViewModel;

            // pass in the animation function that can be called whenever a game changes
            mainWindowViewModel.GameChangeFunction = DoAnimateGameChange;

            // pass in the function that can be called whenever the featured game function changes
            mainWindowViewModel.FeatureChangeFunction = ChangedFeatureSetting;

            // pass in a function that will stop animations and videos when games are started or voice recognition is happening
            mainWindowViewModel.StopVideoAndAnimationsFunction = StopVideoAndAnimations;

            // sets up the game lists and voice recognition
            mainWindowViewModel.InitializeData();
        }

        // when the AttractModeDelay elapses, start into attract mode 
        private void AttractModeDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            mainWindowViewModel.SetPreAttractModeGame();

            Dispatcher.Invoke(() =>
            {
                // reset the background image - it should already be faded out but make sure
                Image_AttractModeBackgroundImage.Opacity = 0;
                Image_AttractModeBackgroundImage.Source = null;
                Image_AttractModeBackgroundImage.RenderTransform = null;

                Image_AttractModeClearLogo.Opacity = 0;
                Image_AttractModeClearLogo.Source = null;

                // make the grid transparent so we can fade it in 
                Grid_AttractMode.Opacity = 0;

                // flag attract mode 
                mainWindowViewModel.IsDisplayingAttractMode = true;

                // fade the grid in if it isn't already
                FadeFrameworkElementOpacity(Grid_AttractMode, 1, 1000);

                // start timer to delay until the next image will fade in 
                attractModeImageFadeInDelay.Start();
            });
        }

        // after delay (about 1 second) - fade in the attract mode background image
        private void AttractModeImageFadeInDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // reset the background image - it should already be faded out but make sure
                Image_AttractModeBackgroundImage.Opacity = 0;
                Image_AttractModeBackgroundImage.Source = null;
                Image_AttractModeBackgroundImage.RenderTransform = null;

                Image_AttractModeClearLogo.Opacity = 0;
                Image_AttractModeClearLogo.Source = null;

                mainWindowViewModel.NextAttractModeGame();

                // get the next image to use for a attract mode background
                activeAttractModeBackgroundImage = new BitmapImage(mainWindowViewModel.CurrentGameList.Game1.BackgroundImage);
                
                // assign the image and fade it in
                if (activeAttractModeBackgroundImage != null)
                {
                    Image_AttractModeBackgroundImage.Source = activeAttractModeBackgroundImage;
                    FadeFrameworkElementOpacity(Image_AttractModeBackgroundImage, 1, 3000);
                }

                // TODO: start 4-5 sec timer and then fade in clear logo
                activeAttractModeClearLogo = new BitmapImage(mainWindowViewModel.CurrentGameList.Game1.ClearLogo);
                if (activeAttractModeClearLogo != null)
                {
                    Image_AttractModeClearLogo.Source = activeAttractModeClearLogo;
                    FadeFrameworkElementOpacity(Image_AttractModeClearLogo, 1, 6000);
                }

                ShiftFrameworkElement(Image_AttractModeBackgroundImage, -100, 17 * 1000);

                // start the delay between attract mode games 
                attractModeChangeDelay.Start();
            });
        }

        // when the AttractModeChangeDelay elapses, change games and continue attract mode
        private void AttractModeChangeDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // fade out this image 
                FadeFrameworkElementOpacity(Image_AttractModeBackgroundImage, 0, 3000);
                FadeFrameworkElementOpacity(Image_AttractModeClearLogo, 0, 500);

                // start timder to dlay until the next image will fade in
                attractModeImageFadeInDelay.Start();
            });
        }

        private void DoAttractMode()
        {

            // animations
            // todo: everything - fade to black approx 1/2 second
            // todo: BG image - fade in approx 1/2 second
            // todo: BG image - alternate shifting - left to right, then right to left 

            // todo: BG image - fade to black approx after 15 seconds

            // todo: place clear logo at the bottom 1/3
            // todo: place genres (separated by grey circles) below clear logo?  
            // todo: clear logo & genre - fade in clear logo and genres after 2 seconds?
            // todo: clear logo & genre - fade out clear logo and genres after 8 seconds? 
            // todo: stop everything on any button press 

            // start change game timer
            attractModeChangeDelay.Start();
        }

        public static LinearGradientBrush GetOpacityBrush()
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint = new Point(1, 0.5);
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.00));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 0.20));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 0.80));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 1.00));
            brush.Freeze();
            return brush;
        }

        public delegate void SetupGameImageDelegate();

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
            return mainWindowViewModel.DoEscape();
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

        private void PlayVideo(MediaElement video, double volume = 0.5)
        {
            if (video != null)
            {
                video.Volume = volume;
                video.Position = TimeSpan.FromMilliseconds(0);
                video.Play();
            }
        }

        private void DimBackground()
        {
            FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 0.25, 25);
            FadeFrameworkElementOpacity(Image_Selected_Background_Black, 1, 25);
            FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 0, 25);
        }

        // animates change in opacity to specified opacity value and given duration
        private void FadeFrameworkElementOpacity(FrameworkElement element, double newOpacityValue, double durationInMilliseconds)
        {
            if (element.Opacity != newOpacityValue)
            {
                DoubleAnimation dimElement = new DoubleAnimation(element.Opacity, newOpacityValue, TimeSpan.FromMilliseconds(durationInMilliseconds));
                element.BeginAnimation(OpacityProperty, dimElement);
            }
        }

        private void ShiftFrameworkElement(FrameworkElement element, double newValue, double durationInMilliseconds)
        {
            Dispatcher.Invoke(() =>
            {
                TranslateTransform translateTransform = new TranslateTransform();
                element.RenderTransform = translateTransform;

                DoubleAnimation moveElement = new DoubleAnimation(0, newValue, TimeSpan.FromMilliseconds(durationInMilliseconds));

                translateTransform.BeginAnimation(TranslateTransform.XProperty, moveElement);
            });
        }

        // todo: do this with a background worker and keep trying to stop for a few seconds?  maybe only in certain cases like launching into a game or escaping to settings menu
        private void StopEverything()
        {
            // pause the video
            PauseVideo(Video_SelectedGame);

            // stop timers
            attractModeDelay.Stop();
            attractModeImageFadeInDelay.Stop();
            attractModeChangeDelay.Stop();
            fadeOutForMovieDelay.Stop();
            backgroundImageChangeDelay.Stop();
            BackgroundImageFadeInSlowStoryBoard.Stop();
        }

        private void DoAnimateGameChange()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if(mainWindowViewModel.IsDisplayingResults)
                    {
                        // stop animations
                        StopEverything();

                        // dim background image
                        DimBackground();

                        // dim logo image
                        FadeFrameworkElementOpacity(Image_Displayed_GameClearLogo, 0.15, 25);

                        // dim game details
                        FadeFrameworkElementOpacity(Grid_SelectedGameDetails, 0.15, 25);

                        // get a handle on the active game's background image
                        activeBackgroundImage = null;
                        Uri uri = mainWindowViewModel?.CurrentGameList?.Game1?.BackgroundImage;
                        if (uri != null)
                        {
                            activeBackgroundImage = new BitmapImage(uri);
                        }

                        // get a handle on the active game's logo image 
                        activeClearLogo = null;
                        Uri clearLogoUri = mainWindowViewModel?.CurrentGameList?.Game1?.ClearLogo;
                        if(clearLogoUri != null)
                        {
                            activeClearLogo = new BitmapImage(clearLogoUri);
                        }

                        // get a handle on the active game's details
                        activeMatchPercentageText = mainWindowViewModel?.CurrentGameList?.Game1?.MatchDescription;
                        activeReleaseYearText = mainWindowViewModel?.CurrentGameList?.Game1?.ReleaseYear;
                        activeCommunityStarRatingImage = null;
                        activePlayModeImage = null;
                        activePlatformLogoImage = null;
                        activeGameBezelImage = null;

                        Uri communityStarRatingUri = mainWindowViewModel?.CurrentGameList?.Game1?.CommunityStarRatingImage;
                        if(communityStarRatingUri != null)
                        {
                            activeCommunityStarRatingImage = new BitmapImage(communityStarRatingUri);
                        }

                        Uri playModeUri = mainWindowViewModel?.CurrentGameList?.Game1?.PlayModeImage;
                        if(playModeUri != null)
                        {
                            activePlayModeImage = new BitmapImage(playModeUri);
                        }

                        Uri platformLogoUri = mainWindowViewModel?.CurrentGameList?.Game1.PlatformClearLogoImage;
                        if(platformLogoUri != null)
                        {
                            activePlatformLogoImage = new BitmapImage(platformLogoUri);
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

                    // fade in the active clear logo
                    Image_Displayed_GameClearLogo.Source = activeClearLogo;
                    FadeFrameworkElementOpacity(Image_Displayed_GameClearLogo, 1, 500);

                    // fade in the active game details 
                    Image_CommunityStarRating.Source = activeCommunityStarRatingImage;
                    Image_Playmode.Source = activePlayModeImage;
                    TextBlock_MatchPercentage.Text = activeMatchPercentageText;
                    TextBlock_ReleaseYear.Text = activeReleaseYearText;
                    Image_PlatformLogo.Source = activePlatformLogoImage;
                    Image_Bezel.Source = activeGameBezelImage;
                    FadeFrameworkElementOpacity(Grid_SelectedGameDetails, 1, 500);
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

                    // start timer to delay to fade out image and play video if there is a video
                    if (Video_SelectedGame?.Source != null)
                    {
                        fadeOutForMovieDelay.Start();
                    }
                    else
                    {
                        attractModeDelay.Start();
                    }
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

                        // fade background images while the video plays
                        FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 0, 1000);
                        FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 0, 1000);
                        FadeFrameworkElementOpacity(Image_Selected_Background_Black, 0, 1000);
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
                    Video_SelectedGame.Stop();

                    // fade in background image
                    FadeFrameworkElementOpacity(Image_Selected_Background_Black, 1, 500);
                    FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 1, 500);
                    attractModeDelay.Start();
                });
            }
            catch(Exception ex)
            {
                Helpers.LogException(ex, "Video_SelectedGame_MediaEnded");
            }
        }

        public void ChangedFeatureSetting()
        {
            // TODO: remove this?
        }

        BackgroundWorker stopVideoAndAnimationWorker;
        private void SetupStopVideoAndAnimationWorker()
        {
            stopVideoAndAnimationWorker = new BackgroundWorker();
            stopVideoAndAnimationWorker.DoWork += StopVideoAndAnimationHandler;
        }

        public void StopVideoAndAnimations()
        {
            stopVideoAndAnimationWorker.RunWorkerAsync();
        }

        void StopVideoAndAnimationHandler(object sender, DoWorkEventArgs e)
        {
            // every 10th of a second for 1 seconds, try to stop everything
            for(int i = 0; i < 10; i++)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        StopEverything();
                    }
                    catch (Exception ex)
                    {
                        Helpers.LogException(ex, "StopVideoAndAnimationWorker");
                    }
                });
                System.Threading.Thread.Sleep(100);
            }
        }

        // setup fallback bezels once the media opens so we can identify whether we need the horizontal or veritical bezel
        private void Video_SelectedGame_MediaOpened(object sender, RoutedEventArgs e)
        {
            Uri gameBezelUri = mainWindowViewModel?.CurrentGameList?.Game1?.GameBezelImage;
            if (gameBezelUri == null)
            {
                // fall back to platform or default bezel if no game bezel, based on height/width of game video if(width >= height) use horizontal, else use vertical
                // do not load bezel if aspect ratio  16:9 (width/height > 1.7)
                if (Video_SelectedGame.NaturalVideoHeight != 0)
                {
                    if((float)((float)(Video_SelectedGame.NaturalVideoWidth / (float)Video_SelectedGame.NaturalVideoHeight)) < 1.7)
                    {
                        BezelOrientation defaultBezelOrientation = BezelOrientation.Horizontal;
                        if (Video_SelectedGame.NaturalVideoWidth < Video_SelectedGame.NaturalVideoHeight)
                        {
                            defaultBezelOrientation = BezelOrientation.Vertical;
                        }
                        gameBezelUri = mainWindowViewModel.GetDefaultBezel(BezelType.PlatformDefault, defaultBezelOrientation, mainWindowViewModel.CurrentGameList.Game1.Game.Platform);
                    }
                }
            }

            if (gameBezelUri != null)
            {
                activeGameBezelImage = new BitmapImage(gameBezelUri);
            }

            // set the opacity mask on the bezel if it's setup or on the video if no bezel
            if (activeGameBezelImage != null)
            {
                Image_Bezel.OpacityMask = opacityBrush;
                Video_SelectedGame.OpacityMask = null;
            }
            else
            {
                Image_Bezel.OpacityMask = null;
                Video_SelectedGame.OpacityMask = opacityBrush;
            }
        }
    }
}
