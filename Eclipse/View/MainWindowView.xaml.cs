using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using System;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.View
{
    public partial class MainWindowView : UserControl, IBigBoxThemeElementPlugin
    {
        private readonly AttractModeService attractModeService;
        private readonly MainWindowViewModel mainWindowViewModel;

        private BackgroundWorker stopVideoAndAnimationWorker;

        private readonly Timer backgroundImageChangeDelay;
        private readonly Timer fadeOutForMovieDelay;

        private BitmapImage activeBackgroundImage;
        private BitmapImage activeClearLogo;
        private BitmapImage activePlayModeImage;
        private BitmapImage activePlatformLogoImage;
        private BitmapImage activeGameBezelImage;

        private string activeMatchPercentageText;
        private string activeReleaseYearText;
        private string activeGameTitleText;


        private bool disableVideos;

        public MainWindowView()
        {
            InitializeComponent();

            SetupStopVideoAndAnimationWorker();


            // create a timer to delay swapping background images
            backgroundImageChangeDelay = new Timer(1000);
            backgroundImageChangeDelay.Elapsed += FadeInCurrentGame;
            backgroundImageChangeDelay.AutoReset = false;

            // create a timer to delay playing movie and fading out background images 
            if (EclipseSettingsDataProvider.Instance?.EclipseSettings?.VideoDelayInMilliseconds > 0)
            {
                fadeOutForMovieDelay = new Timer(EclipseSettingsDataProvider.Instance.EclipseSettings.VideoDelayInMilliseconds);
                fadeOutForMovieDelay.Elapsed += FadeOutForMovieDelay_Elapsed;
                fadeOutForMovieDelay.AutoReset = false;
            }

            // get handle on settings
            disableVideos = EclipseSettingsDataProvider.Instance?.EclipseSettings?.DisableVideos == true;

            // get handle on the view model 
            mainWindowViewModel = DataContext as MainWindowViewModel;

            // pass in the animation function that can be called whenever a game changes
            mainWindowViewModel.GameChangeFunction = DoAnimateGameChange;

            // pass in a function that will stop animations and videos when games are started or voice recognition is happening
            mainWindowViewModel.StopVideoAndAnimationsFunction = StopVideoAndAnimations;


            attractModeService = AttractModeService.Instance;
            attractModeService.MainWindowViewModel = mainWindowViewModel;
            attractModeService.Presenter = AttractModeView_Control;

        }


        public bool OnDown(bool held)
        {
            return mainWindowViewModel.DoDown(held);
        }

        public bool OnEnter()
        {
            return mainWindowViewModel.DoEnter();
        }

        public bool OnEscape()
        {
            return mainWindowViewModel.DoEscape();
        }

        public bool OnLeft(bool held)
        {
            return mainWindowViewModel.DoLeft(held);
        }

        public bool OnPageDown()
        {
            return mainWindowViewModel.DoPageDown();
        }

        public bool OnPageUp()
        {
            return mainWindowViewModel.DoPageUp();
        }

        public bool OnRight(bool held)
        {
            return mainWindowViewModel.DoRight(held);
        }

        public void OnSelectionChanged(FilterType filterType, string filterValue, IPlatform platform, IPlatformCategory category, IPlaylist playlist, IGame game)
        {
        }

        public bool OnUp(bool held)
        {
            return mainWindowViewModel.DoUp(held);
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
            if ((mainWindowViewModel.IsPlayingGame == false) && (video != null))
            {
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
        private void FadeFrameworkElementOpacity(FrameworkElement element, double newOpacityValue, double durationInMilliseconds, EventHandler completedEventHandler = null)
        {
            if (element.Opacity != newOpacityValue)
            {
                DoubleAnimation dimElement = new DoubleAnimation(element.Opacity, newOpacityValue, TimeSpan.FromMilliseconds(durationInMilliseconds));

                if (completedEventHandler != null)
                {
                    dimElement.Completed += completedEventHandler;
                }

                element.BeginAnimation(OpacityProperty, dimElement);
            }
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

                        // StopEverything also switches off the idle timer. That is right when
                        // a game is launching, but changing the selected game is ordinary
                        // browsing, so re-arm it. Previously the timer was left stopped here
                        // and only a video's MediaEnded turned it back on - so for a game with
                        // no preview video the screen saver never started at all.
                        attractModeService.RestartAttractMode();

                        // dim background image
                        DimBackground();

                        // dim logo image
                        Image_Displayed_GameClearLogo.Opacity = 0.15;
                        FadeFrameworkElementOpacity(Image_Displayed_GameClearLogo, 0.15, 25);

                        // dim game title text all the way to 0 
                        // this is a backup for missing logo image
                        TextBlock_Displayed_GameTitle.Opacity = 0;
                        FadeFrameworkElementOpacity(TextBlock_Displayed_GameTitle, 0.00, 25);

                        // dim game details
                        Grid_SelectedGameDetails.Opacity = 0.15;
                        FadeFrameworkElementOpacity(Grid_SelectedGameDetails, 0.15, 25);

                        // get a handle on the active game's background image
                        activeBackgroundImage = null;
                        Uri uri = mainWindowViewModel?.CurrentGameList?.Game1?.GameFiles?.BackgroundImage;
                        if (uri != null)
                        {
                            activeBackgroundImage = new BitmapImage(uri);
                        }

                        // get a handle on the active game's logo image 
                        activeClearLogo = null;
                        Uri clearLogoUri = mainWindowViewModel?.CurrentGameList?.Game1?.GameFiles?.ClearLogo;
                        if(clearLogoUri != null)
                        {
                            activeClearLogo = new BitmapImage(clearLogoUri);
                        }

                        // get a handle on the active game's details
                        activeGameTitleText = mainWindowViewModel?.CurrentGameList?.Game1?.Game?.Title;
                        activeMatchPercentageText = mainWindowViewModel?.CurrentGameList?.Game1?.MatchDescription;
                        activeReleaseYearText = mainWindowViewModel?.CurrentGameList?.Game1?.ReleaseYear;
                        activePlayModeImage = null;
                        activePlatformLogoImage = null;
                        activeGameBezelImage = null;

                        Uri playModeUri = mainWindowViewModel?.CurrentGameList?.Game1?.GameFiles?.PlayModeImage;
                        if(playModeUri != null)
                        {
                            activePlayModeImage = new BitmapImage(playModeUri);
                        }

                        Uri platformLogoUri = mainWindowViewModel?.CurrentGameList?.Game1?.GameFiles?.PlatformClearLogoImage;
                        if(platformLogoUri != null)
                        {
                            activePlatformLogoImage = new BitmapImage(platformLogoUri);
                        }

                        Uri bezelUri = mainWindowViewModel?.CurrentGameList?.Game1?.GameFiles.GameBezelImage;
                        if (bezelUri != null)
                        {
                            activeGameBezelImage = new BitmapImage(bezelUri);
                        }

                        // start the timer - when it goes off, fade in the new background image
                        // this delay waits until inputs are idle for 1 second before fading in the current selected game
                        // so that the game details and images do not update immediately when you hold down left or right
                        // when a new game is selected the timer is restarted so you have to be idle for a full second to trigger the game change
                        backgroundImageChangeDelay.Start();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MainWindowView.xaml.cs.DoAnimateGameChange");
                }
            });
        }

        // when the above timer completes we swap to the current selected game 
        public void FadeInCurrentGame(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    FadeInCurrentGameDetails();
                    FadeForMovie();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "BackgroundImageChangeDelay_Elapsed");
                }
            });
        }

        private void FadeInCurrentGameDetails()
        {
            if (Image_Active_BackgroundImage != null)
            {
                Image_Active_BackgroundImage.Opacity = 0;
                Image_Active_BackgroundImage.Source = activeBackgroundImage;
            }

            // fade in the active clear logo
            Image_Displayed_GameClearLogo.Source = activeClearLogo;
            FadeFrameworkElementOpacity(Image_Displayed_GameClearLogo, 1, 500);

            // fade in the game title 
            TextBlock_Displayed_GameTitle.Text = activeGameTitleText;
            FadeFrameworkElementOpacity(TextBlock_Displayed_GameTitle, 1, 500);

            // fade in the active game details 
            Image_Playmode.Source = activePlayModeImage;
            TextBlock_MatchPercentage.Text = activeMatchPercentageText;
            TextBlock_ReleaseYear.Text = activeReleaseYearText;
            Image_PlatformLogo.Source = activePlatformLogoImage;
            Image_Bezel.Source = activeGameBezelImage;

            // fade in the game details 
            FadeFrameworkElementOpacity(Grid_SelectedGameDetails, 1, 500);
        }

        private void FadeForMovie()
        {
            // todo: if video delay timer is 0, switch the order - play video and do not fade in background? 
            if ((Video_SelectedGame?.Source != null) && (fadeOutForMovieDelay == null))
            {
                // straight to playing video 
                FadeOutForMovieDelay_Elapsed(this, null);
            }
            else
            {
                // fade in the active background image 
                FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 1, 500, BackgroundImageFadeIn_Completed);
            }
        }

        private void BackgroundImageFadeIn_Completed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // set displayed background image control to the current active image
                Image_Displayed_BackgroundImage.Source = Image_Active_BackgroundImage.Source;

                // hide the active image control
                Image_Active_BackgroundImage.Opacity = 0;

                // start timer to delay to fade out image and play video if there is a video
                if (Video_SelectedGame?.Source != null)
                {
                    if (fadeOutForMovieDelay != null)
                    {
                        fadeOutForMovieDelay.Start();
                    }
                }
            });
        }

        private void FadeOutForMovieDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Only hand the idle timer over to the video if one is actually going to
                    // play. Nothing but MediaEnded turns it back on, so stopping it here when
                    // there is no video left the screen saver switched off until the next
                    // keypress. The old guard tested the MediaElement itself, which is never
                    // null, rather than whether it had a source.
                    if (disableVideos
                        || Video_SelectedGame?.Source == null
                        || mainWindowViewModel.IsPlayingGame)
                    {
                        attractModeService.RestartAttractMode();
                        return;
                    }

                    attractModeService.StopAttractMode();

                    PlayVideo(Video_SelectedGame);

                    // fade background images while the video plays
                    FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 0, 1000, SwapBackgroundImages);
                    FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 0, 1000);
                    FadeFrameworkElementOpacity(Image_Selected_Background_Black, 0, 1000);
                });
            }
            catch(Exception ex)
            {
                LogHelper.LogException(ex, "FadeOutForMovieDelay_Elapsed");
            }
        }

        private void SwapBackgroundImages(object sender, EventArgs e)
        {
            // set displayed background image control to the current active image
            Image_Displayed_BackgroundImage.Source = Image_Active_BackgroundImage.Source;

            // hide the active image control
            Image_Active_BackgroundImage.Opacity = 0;
        }

        private void Video_SelectedGame_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Video_SelectedGame.Stop();

                    FadeInBackgroundImages();

                    attractModeService.RestartAttractMode();
                });
            }
            catch(Exception ex)
            {
                LogHelper.LogException(ex, "Video_SelectedGame_MediaEnded");
            }
        }

        // A video that never loads would otherwise never raise MediaEnded, leaving the idle
        // timer switched off for the rest of the session.
        private void Video_SelectedGame_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            try
            {
                LogHelper.LogException(e?.ErrorException, "play the selected game's video");

                Dispatcher.Invoke(() =>
                {
                    FadeInBackgroundImages();
                    attractModeService.RestartAttractMode();
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Video_SelectedGame_MediaFailed");
            }
        }

        private void FadeInBackgroundImages()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // make sure the displayed background image source is set to the active image before fading in
                    // set displayed background image control to the current active image
                    Image_Displayed_BackgroundImage.Source = Image_Active_BackgroundImage.Source;

                    // hide the active image control
                    Image_Active_BackgroundImage.Opacity = 0;

                    // fade in background image
                    FadeFrameworkElementOpacity(Image_Selected_Background_Black, 1, 500);
                    FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 1, 500);
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "FadeInBackgroundImages");
            }
        }



        // setup fallback bezels once the media opens so we can identify whether we need the horizontal or veritical bezel
        private void Video_SelectedGame_MediaOpened(object sender, RoutedEventArgs e)
        {
            Uri gameBezelUri = mainWindowViewModel?.CurrentGameList?.Game1?.GameFiles?.GameBezelImage;
            if (gameBezelUri == null)
            {
                // fall back to platform or default bezel if no game bezel, based on height/width of game video if(width >= height) use horizontal, else use vertical
                // do not load bezel if aspect ratio  16:9 (width/height > 1.7)
                if (Video_SelectedGame.NaturalVideoHeight != 0)
                {
                    if ((float)((float)(Video_SelectedGame.NaturalVideoWidth / (float)Video_SelectedGame.NaturalVideoHeight)) < 1.7)
                    {
                        BezelOrientation defaultBezelOrientation = BezelOrientation.Horizontal;
                        if (Video_SelectedGame.NaturalVideoWidth < Video_SelectedGame.NaturalVideoHeight)
                        {
                            defaultBezelOrientation = BezelOrientation.Vertical;
                        }

                        gameBezelUri = BezelService.Instance.GetDefaultBezel(BezelType.PlatformDefault, defaultBezelOrientation, mainWindowViewModel.CurrentGameList.Game1.Game.Platform);
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
                Image_Bezel.OpacityMask = OpacityBrushHelper.Instance.OpacityBrush;
                Video_SelectedGame.OpacityMask = null;
            }
            else
            {
                Image_Bezel.OpacityMask = null;
                Video_SelectedGame.OpacityMask = OpacityBrushHelper.Instance.OpacityBrush;
            }
        }

        private void SetupStopVideoAndAnimationWorker()
        {
            stopVideoAndAnimationWorker = new BackgroundWorker();
            stopVideoAndAnimationWorker.DoWork += StopVideoAndAnimationHandler;
        }

        private void StopVideoAndAnimations()
        {
            stopVideoAndAnimationWorker.RunWorkerAsync();
        }

        private void StopVideoAndAnimationHandler(object sender, DoWorkEventArgs e)
        {
            // every 10th of a second for half a second, try to stop everything
            for (int i = 0; i < 5; i++)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        StopEverything();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex, "StopVideoAndAnimationWorker");
                    }
                });
                System.Threading.Thread.Sleep(100);
            }

            // reset everything to the active game 
            Dispatcher.Invoke(() =>
            {
                // FadeInCurrentGame(this, null);
                FadeInCurrentGameDetails();
                FadeInBackgroundImages();
            });
        }

        // maybe only in certain cases like launching into a game or escaping to settings menu
        private void StopEverything()
        {
            AttractModeService.Instance.StopAttractMode();

            // pause the video
            PauseVideo(Video_SelectedGame);

            // stop timers
            if (backgroundImageChangeDelay != null)
            {
                backgroundImageChangeDelay.Stop();
            }

            if (fadeOutForMovieDelay != null)
            {
                fadeOutForMovieDelay.Stop();
            }
        }
    }
}
