using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.View
{
    public partial class MainWindowView : UserControl, IBigBoxThemeElementPlugin
    {
        private readonly AttractModeService attractModeService;
        private readonly MainWindowViewModel mainWindowViewModel;

        // How long the selection has to be still before its media is loaded and shown, and how
        // long the background artwork takes to fade in behind the details and out behind a video.
        private const int SettleDelayMilliseconds = 1000;
        private const int BackgroundFadeInMilliseconds = 500;
        private const int BackgroundFadeOutMilliseconds = 1000;

        // Cancels the selection sequence in flight. Everything from the settle delay to the
        // video runs as one sequence per selection, so a new selection - or a game launching -
        // cancels the whole thing rather than trying to catch it a step at a time.
        private CancellationTokenSource selectionCancellation;

        // 0 or less means no pause between the artwork and the video.
        private readonly int videoDelayMilliseconds;

        // Decoded artwork for the game the selection settled on, and the paths it came from.
        // The paths are captured on every game change because reading them is free; the decode
        // waits for the settle and happens off the UI thread. It used to run synchronously on
        // the UI thread for every game scrolled past - five files per keypress, all discarded.
        private ImageSource activeBackgroundImage;
        private ImageSource activeClearLogo;
        private ImageSource activePlayModeImage;
        private ImageSource activePlatformLogoImage;
        private ImageSource activeGameBezelImage;

        private Uri activeBackgroundUri;
        private Uri activeClearLogoUri;
        private Uri activePlayModeUri;
        private Uri activePlatformLogoUri;
        private Uri activeGameBezelUri;

        private string activeMatchPercentageText;
        private string activeReleaseYearText;
        private string activeGameTitleText;

        // The video file for the game the selection has settled on. Held here rather than read
        // back off the player, because MediaElement.Source no longer tracks the selected game -
        // it is only set once the selection settles, so scrolling past a game does not open it.
        private string activeVideoPath;

        private readonly VideoPlaybackMonitor videoMonitor = new VideoPlaybackMonitor();

        // Where the file in the player has got to. MediaOpened arrives after Play(), not before
        // it - LoadedBehavior is Manual, so nothing is loaded until playback is asked for - and
        // the check on whether playback started has to be timed from the open, not from Play().
        private bool videoIsOpen;
        private bool videoPlayRequested;

        // One missed playback check is not proof of anything. A run of them with nothing playing
        // in between is, and ten in a row means the media stack is not coming back.
        private const int RecoveryFailureThreshold = 3;
        private const int AbandonFailureThreshold = 10;

        // Fires shortly after Play() to check whether playback actually began. A DispatcherTimer
        // rather than System.Timers.Timer: this only ever touches the player and runs on the UI
        // thread already.
        private readonly DispatcherTimer playbackStartCheck;

        private bool videoAbandonedForSession;

        private bool disableVideos;

        public MainWindowView()
        {
            InitializeComponent();

            // how long the new game's artwork holds the screen before the video takes over
            videoDelayMilliseconds = EclipseSettingsDataProvider.Instance?.EclipseSettings?.VideoDelayInMilliseconds ?? 0;

            playbackStartCheck = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PlaybackStartCheckMilliseconds)
            };
            playbackStartCheck.Tick += PlaybackStartCheck_Tick;

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

            // The theme is composed on a 32 x 18 grid whose cells are square at 16:9. Which
            // rows are held to that square unit and which absorb a taller display's surplus is
            // decided in ApplyStageGeometry, and it has to run whenever the host resizes us.
            SizeChanged += (sender, args) => ApplyStageGeometry();
        }

        /// <summary>
        /// Holds the 16:9 composition still, and gives a taller display's surplus height to the
        /// game list underneath it.
        ///
        /// The theme is composed on a 32 x 18 grid whose cells are square at 16:9 - 60 x 60 at
        /// 1080p. Rows 0-17 are given that square unit outright, so everything placed in them -
        /// the background artwork, the video and its bezel, the clear logo, the game details -
        /// keeps its 16:9 size and position on any display. The nineteenth row is star sized and
        /// takes whatever is left: nothing at 16:9, 120px at 1920x1200. Only the game list and
        /// the full screen overlays reach into it.
        ///
        /// The current list band is then held to its own 16:9 height, so the surplus arrives in
        /// the next-list teaser below it rather than making the box art bigger. That matters
        /// beyond appearance: box art is pre-scaled on disk to the height it is rendered at, and
        /// ImageScaler sizes it from the same LayoutGeometry expression used here.
        ///
        /// On a 16:9 display width/32 and height/18 are the same number, so every value assigned
        /// here equals what the star sizing it replaces already produced, at any resolution.
        /// </summary>
        private void ApplyStageGeometry()
        {
            double unit = LayoutGeometry.Unit(ActualWidth, ActualHeight);
            if (unit <= 0)
            {
                // no size yet - SizeChanged will bring us back
                return;
            }

            GridLength stageRow = new GridLength(unit);
            PinDesignStageRows(Grid_DisplayingResults, stageRow);
            PinDesignStageRows(Grid_VoiceSearch, stageRow);

            // The voice search overlay re-declares this layout so the heard phrase lands on the
            // list heading it is about to become (see the note above it in the markup). It is
            // driven from the same numbers here rather than being kept in step by hand.
            GridLength listBand = new GridLength(LayoutGeometry.CurrentListBandHeight(unit));
            SetRowHeight(GameListGrid, 0, listBand);
            SetRowHeight(Grid_VoiceSearchListArea, 0, listBand);
        }

        // Rows 0..17 are the design stage. The row after them is left star sized to collect the
        // surplus, which is why this stops short of the collection's end.
        private static void PinDesignStageRows(Grid grid, GridLength stageRow)
        {
            int rows = Math.Min(LayoutGeometry.DesignRows, grid.RowDefinitions.Count);

            for (int row = 0; row < rows; row++)
            {
                SetRowHeight(grid, row, stageRow);
            }
        }

        // Assigning a height invalidates the grid's layout, so only assign one that changed.
        private static void SetRowHeight(Grid grid, int row, GridLength height)
        {
            if (row < grid.RowDefinitions.Count && grid.RowDefinitions[row].Height != height)
            {
                grid.RowDefinitions[row].Height = height;
            }
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


        // Long enough that a healthy file has certainly advanced past zero, short enough that a
        // failure is recorded while the same game is still selected.
        private const double PlaybackStartCheckMilliseconds = 1500;

        /// <summary>
        /// Whether the game the selection settled on has a video worth playing. This used to be
        /// read off MediaElement.Source, which only worked because Source was bound to the
        /// selected game - the very binding that made every scrolled-past game open a file.
        /// </summary>
        private bool HasVideoForSettledGame =>
            !disableVideos
            && !videoAbandonedForSession
            && !string.IsNullOrWhiteSpace(activeVideoPath);

        /// <summary>
        /// Hands a file to the player. Close() first: MediaElement is a wrapper over the legacy
        /// Windows media stack, which does not reliably release a file when Source is simply
        /// reassigned, and this used to happen once per game scrolled past.
        ///
        /// Assigning Source does not load anything. LoadedBehavior is Manual, so the file is not
        /// opened until Play() is called and MediaOpened arrives after that, not before it.
        /// </summary>
        private void OpenVideo(string videoPath)
        {
            if (Video_SelectedGame == null)
            {
                return;
            }

            videoIsOpen = false;
            videoPlayRequested = false;

            Video_SelectedGame.Close();

            if (string.IsNullOrWhiteSpace(videoPath) || videoAbandonedForSession)
            {
                Video_SelectedGame.Source = null;
                return;
            }

            videoMonitor.OpenRequested(videoPath);
            Video_SelectedGame.Source = new Uri(videoPath);
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

                videoMonitor.PlayRequested();
                videoPlayRequested = true;

                // Normally this is what opens the file, so the check waits for MediaOpened
                // rather than starting here - timing a file that has not loaded yet is what
                // produced failures for videos that went on to play perfectly well.
                if (videoIsOpen)
                {
                    StartPlaybackStartCheck();
                }
            }
        }

        // ask again shortly whether anything actually happened
        private void StartPlaybackStartCheck()
        {
            playbackStartCheck.Stop();
            playbackStartCheck.Start();
        }

        private void PlaybackStartCheck_Tick(object sender, EventArgs e)
        {
            playbackStartCheck.Stop();

            if (Video_SelectedGame?.Source == null)
            {
                return;
            }

            if (Video_SelectedGame.Position > TimeSpan.Zero)
            {
                videoMonitor.PlayStarted();
                return;
            }

            // Play was requested and the position never moved. This is the failure that only
            // appears after a long session, so record it with the running totals.
            videoMonitor.PlayDidNotStart();
            ApplyFailurePolicy();
        }

        /// <summary>
        /// Decides what a failure is worth. Every one is logged, because the log is the only
        /// witness to a bug that takes hours to appear - but nothing is done about a failure on
        /// its own. Only a run of them with no playback in between is evidence that the media
        /// stack itself is in trouble, and any successful playback clears the run.
        /// </summary>
        private void ApplyFailurePolicy()
        {
            if (videoAbandonedForSession)
            {
                return;
            }

            int consecutiveFailures = videoMonitor.ConsecutiveFailures;

            if (consecutiveFailures >= AbandonFailureThreshold)
            {
                videoAbandonedForSession = true;
                videoMonitor.RecoveryAbandoned();
                return;
            }

            if (consecutiveFailures < RecoveryFailureThreshold)
            {
                return;
            }

            videoMonitor.RecoveryAttempted();

            videoIsOpen = false;
            videoPlayRequested = false;

            Video_SelectedGame?.Close();

            if (Video_SelectedGame != null)
            {
                Video_SelectedGame.Source = null;
            }
        }

        private void DimBackground()
        {
            FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 0.25, 25);
            FadeFrameworkElementOpacity(Image_Selected_Background_Black, 1, 25);
            FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 0, 25);
        }

        /// <summary>
        /// Animates change in opacity to specified opacity value and given duration.
        ///
        /// This used to skip the animation when the element was already at the target opacity,
        /// which sounds like an optimisation and is not. Reading Opacity gives the animated
        /// value, and an animation that has only just been started still reads as its starting
        /// value - so "already there" could mean "on its way somewhere else". A fade asked for
        /// in that moment was dropped, and the element finished wherever the earlier animation
        /// was heading. Always animating costs a no-op clock and makes the last caller win.
        /// </summary>
        private void FadeFrameworkElementOpacity(FrameworkElement element, double newOpacityValue, double durationInMilliseconds)
        {
            DoubleAnimation dimElement = new DoubleAnimation(element.Opacity, newOpacityValue, TimeSpan.FromMilliseconds(durationInMilliseconds));

            element.BeginAnimation(OpacityProperty, dimElement);
        }

        /// <summary>
        /// Sets opacity outright, clearing any animation that is still holding the property.
        /// A finished WPF animation keeps ownership of the value it ended on, so a plain
        /// assignment is silently ignored - which is how an image told to hide could stay
        /// visible, and how the fade that follows could be skipped for already being there.
        /// </summary>
        private static void SetOpacity(FrameworkElement element, double opacity)
        {
            element.BeginAnimation(OpacityProperty, null);
            element.Opacity = opacity;
        }

        private void DoAnimateGameChange()
        {
            Dispatcher.Invoke(() =>
            {
                BrowsePerformanceMonitor monitor = BrowsePerformanceMonitor.Instance;
                Stopwatch animateTimer = monitor.IsEnabled ? Stopwatch.StartNew() : null;

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

                        // Note what the selected game's media is, but do not load any of it yet.
                        // Reading paths and text off the view model is free; decoding five images
                        // is not, and it used to happen here - on the UI thread, for every game
                        // scrolled past, all of it discarded by the next keypress. The decode now
                        // waits for the settle and runs off the UI thread - see
                        // LoadSettledGameMediaAsync.
                        GameMatch settledGame = mainWindowViewModel?.CurrentGameList?.SelectedGame;
                        GameFiles settledFiles = settledGame?.GameFiles;

                        activeBackgroundUri = settledFiles?.BackgroundImage;
                        activeClearLogoUri = settledFiles?.ClearLogo;
                        activePlayModeUri = settledFiles?.PlayModeImage;
                        activePlatformLogoUri = settledFiles?.PlatformClearLogoImage;
                        activeGameBezelUri = settledFiles?.GameBezelImage;
                        activeVideoPath = settledFiles?.VideoPath;

                        // get a handle on the active game's details
                        activeGameTitleText = settledGame?.Game?.Title;
                        activeMatchPercentageText = settledGame?.MatchDescription;
                        activeReleaseYearText = settledGame?.ReleaseYear;

                        // hand the rest of the change over to the selection sequence - it waits
                        // for the inputs to be idle for a second before loading or showing
                        // anything, so holding left or right does not update the game details
                        // and images for every game passed on the way
                        ShowSelectedGameAsync();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MainWindowView.xaml.cs.DoAnimateGameChange");
                }

                if (animateTimer != null)
                {
                    monitor.AnimateGameChangeCompleted(animateTimer.Elapsed.TotalMilliseconds);

                    // Loaded runs after the dispatcher has finished the layout and render pass
                    // this change caused, so it is the closest thing to "the user can see it" -
                    // and it is where a synchronous image decode would show up.
                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(monitor.RenderCompleted));
                }
            });
        }

        /// <summary>
        /// Everything that happens once a game is selected, as one sequence: wait for the
        /// selection to settle, load the media, show the artwork, then hand the screen over to
        /// the video. A new selection cancels the sequence in flight.
        ///
        /// This replaces two thread-pool timers whose steps were chained together by animation
        /// Completed callbacks. Those callbacks could not be cancelled, so a step could run for
        /// a game that was no longer selected - and because the fade helper skips an animation
        /// whose target opacity is already in place, a skipped fade meant a Completed callback
        /// that never fired and a video that never played.
        /// </summary>
        private async void ShowSelectedGameAsync()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            CancellationTokenSource previousSelection = Interlocked.Exchange(ref selectionCancellation, cancellation);
            previousSelection?.Cancel();

            CancellationToken cancellationToken = cancellation.Token;

            try
            {
                // The debounce. Nothing is read from disk and nothing is shown until the
                // selection has been still, so holding a direction through fifty games costs
                // one pass of the work below rather than fifty.
                await Task.Delay(SettleDelayMilliseconds, cancellationToken);

                await LoadSettledGameMediaAsync(cancellationToken);

                // the selection settled here, so this is the first point worth opening a file
                OpenVideo(activeVideoPath);

                FadeInCurrentGameDetails();

                if (HasVideoForSettledGame && (videoDelayMilliseconds <= 0))
                {
                    // no pause configured, so the artwork never gets its moment on screen
                    await PlaySettledGameVideoAsync(cancellationToken);
                    return;
                }

                // fade the new artwork in over the outgoing game's dimmed artwork
                FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 1, BackgroundFadeInMilliseconds);
                await Task.Delay(BackgroundFadeInMilliseconds, cancellationToken);

                SettleBackgroundImage();

                if (!HasVideoForSettledGame)
                {
                    return;
                }

                await Task.Delay(videoDelayMilliseconds, cancellationToken);

                await PlaySettledGameVideoAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // a newer selection took over, or a game is launching - either way this one is
                // no longer the game on screen and has nothing left to do
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MainWindowView.xaml.cs.ShowSelectedGameAsync");
            }
            finally
            {
                Interlocked.CompareExchange(ref selectionCancellation, null, cancellation);
                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Decodes the settled game's artwork away from the UI thread, then hands it over -
        /// unless the selection moved on while the files were being read, in which case this
        /// artwork belongs to a game that is no longer on screen.
        /// </summary>
        private async Task LoadSettledGameMediaAsync(CancellationToken cancellationToken)
        {
            ImageSource background = await FrozenImageLoader.LoadAsync(activeBackgroundUri);
            ImageSource clearLogo = await FrozenImageLoader.LoadAsync(activeClearLogoUri);
            ImageSource playMode = await FrozenImageLoader.LoadAsync(activePlayModeUri);
            ImageSource platformLogo = await FrozenImageLoader.LoadAsync(activePlatformLogoUri);
            ImageSource gameBezel = await FrozenImageLoader.LoadAsync(activeGameBezelUri);

            cancellationToken.ThrowIfCancellationRequested();

            activeBackgroundImage = background;
            activeClearLogo = clearLogo;
            activePlayModeImage = playMode;
            activePlatformLogoImage = platformLogo;
            activeGameBezelImage = gameBezel;
        }

        /// <summary>
        /// Hands the screen over to the video: the screen saver's idle timer stops, playback
        /// starts, and the background artwork fades out behind it.
        /// </summary>
        private async Task PlaySettledGameVideoAsync(CancellationToken cancellationToken)
        {
            // Only hand the idle timer over to the video if one is actually going to play.
            // Nothing but MediaEnded turns it back on, so stopping it when there is no video
            // left the screen saver switched off until the next keypress.
            if (disableVideos
                || !HasVideoForSettledGame
                || mainWindowViewModel.IsPlayingGame)
            {
                attractModeService.RestartAttractMode();
                return;
            }

            attractModeService.StopAttractMode();

            PlayVideo(Video_SelectedGame);

            // fade background images while the video plays
            FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 0, BackgroundFadeOutMilliseconds);
            FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 0, BackgroundFadeOutMilliseconds);
            FadeFrameworkElementOpacity(Image_Selected_Background_Black, 0, BackgroundFadeOutMilliseconds);

            await Task.Delay(BackgroundFadeOutMilliseconds, cancellationToken);

            SwapBackgroundImages();
        }

        private void FadeInCurrentGameDetails()
        {
            if (Image_Active_BackgroundImage != null)
            {
                // outright, not a plain assignment - the fade that follows is skipped when the
                // opacity is already 1, and a held animation would keep it there
                SetOpacity(Image_Active_BackgroundImage, 0);
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

        private void SwapBackgroundImages()
        {
            // set displayed background image control to the current active image
            Image_Displayed_BackgroundImage.Source = Image_Active_BackgroundImage.Source;

            // hide the active image control
            SetOpacity(Image_Active_BackgroundImage, 0);
        }

        /// <summary>
        /// Ends the artwork change: the displayed layer takes over the picture the active layer
        /// just faded in, at full brightness.
        ///
        /// Both layers are showing the same picture here, so the handover itself is invisible -
        /// what matters is the undim. The displayed layer has been at 0.25 since the game change
        /// began, so hiding the active layer without restoring it drops the background straight
        /// back to dim the moment the fade-in finishes.
        /// </summary>
        private void SettleBackgroundImage()
        {
            Image_Displayed_BackgroundImage.Source = Image_Active_BackgroundImage.Source;

            SetOpacity(Image_Displayed_BackgroundImage, 1);
            SetOpacity(Image_Active_BackgroundImage, 0);
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
                videoMonitor.OpenFailed(e?.ErrorException);

                Dispatcher.Invoke(() =>
                {
                    playbackStartCheck.Stop();

                    ApplyFailurePolicy();

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
                    SwapBackgroundImages();

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
            videoMonitor.OpenCompleted();

            // the file is genuinely loaded now, so this is the point to start timing whether
            // playback gets going
            videoIsOpen = true;

            if (videoPlayRequested)
            {
                StartPlaybackStartCheck();
            }

            Uri gameBezelUri = mainWindowViewModel?.CurrentGameList?.SelectedGame?.GameFiles?.GameBezelImage;
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

                        gameBezelUri = BezelService.Instance.GetDefaultBezel(BezelType.PlatformDefault, defaultBezelOrientation, mainWindowViewModel.CurrentGameList.SelectedGame.Game.Platform);
                    }
                }
            }

            if (gameBezelUri != null)
            {
                activeGameBezelImage = new BitmapImage(gameBezelUri);
            }

            // The fallback bezel can only be chosen once the video's dimensions are known, which
            // is here, after the artwork has already been put on screen - so this has to set the
            // source itself. It used to get away without doing that: the Source binding opened
            // the media as soon as the selection changed, so this ran a second before the settle
            // and FadeInCurrentGameDetails picked the field up afterwards.
            Image_Bezel.Source = activeGameBezelImage;

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

        /// <summary>
        /// Called when a game is launching or voice recognition starts: everything in flight has
        /// to stop, and the selected game's artwork has to go back on screen.
        ///
        /// This used to be a BackgroundWorker that called StopEverything five times over half a
        /// second. It had to keep trying because stopping the timers could not stop an animation
        /// Completed callback that had already been queued to start them again. Cancelling the
        /// selection sequence stops it once, so the retry loop is gone.
        /// </summary>
        private void StopVideoAndAnimations()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    StopEverything();

                    // reset everything to the active game
                    FadeInCurrentGameDetails();
                    FadeInBackgroundImages();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "StopVideoAndAnimations");
                }
            });
        }

        // maybe only in certain cases like launching into a game or escaping to settings menu
        private void StopEverything()
        {
            AttractModeService.Instance.StopAttractMode();

            // the check must not outlive the playback attempt it was measuring
            playbackStartCheck?.Stop();

            // pause the video
            PauseVideo(Video_SelectedGame);

            // abandon the selection sequence in flight - otherwise it carries on and starts the
            // video again a moment later
            selectionCancellation?.Cancel();
        }
    }
}
