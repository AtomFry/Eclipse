using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.View
{
    public partial class MainWindowView : UserControl, IBigBoxThemeElementPlugin, ISelectedGamePresenter
    {
        private readonly AttractModeService attractModeService;
        private readonly MainWindowViewModel mainWindowViewModel;

        // Every duration in the selection sequence, from settings. Taken once at construction
        // rather than per game change: nothing in Eclipse applies settings live, and a sequence
        // whose steps disagreed about a duration part-way through would be worse than one that
        // needs a restart.
        private readonly SelectionTimings timings;

        // The sequence that runs on every game change: settle, load, show, then video. It owns
        // the ordering and the cancellation; this class owns only what appears on screen.
        private readonly SelectedGameSequence selectedGameSequence;

        // What ShowGameDetails last put on screen, so StopAndRestore can put it back without
        // asking the sequence for it again.
        private DecodedGameMedia shownMedia;

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

        // Fires shortly after Play() to check whether playback actually began. A DispatcherTimer
        // rather than System.Timers.Timer: this only ever touches the player and runs on the UI
        // thread already.
        private readonly DispatcherTimer playbackStartCheck;

        private bool videoAbandonedForSession;

        private bool disableVideos;

        public MainWindowView()
        {
            InitializeComponent();

            timings = SelectionTimings.Current;

            playbackStartCheck = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PlaybackStartCheckMilliseconds)
            };
            playbackStartCheck.Tick += PlaybackStartCheck_Tick;

            // get handle on settings
            disableVideos = EclipseSettingsDataProvider.Instance?.EclipseSettings?.DisableVideos == true;

            // get handle on the view model 
            mainWindowViewModel = DataContext as MainWindowViewModel;

            // The view listens; the view model does not hold a delegate pointing back here. Both
            // are raised from whatever thread moved the selection - the media pump raises the
            // first from a background thread - so both handlers marshal to the UI thread.
            //
            // Never detached, deliberately: this control is the plugin's entry point and the view
            // model is its DataContext, so the two are created together and live until Big Box
            // exits. There is no point at which one outlives the other to leak from.
            mainWindowViewModel.SelectedGameChanged += (sender, args) => DoAnimateGameChange();
            mainWindowViewModel.PresentationInterrupted += (sender, args) => StopVideoAndAnimations();


            attractModeService = AttractModeService.Instance;
            attractModeService.MainWindowViewModel = mainWindowViewModel;
            attractModeService.Presenter = AttractModeView_Control;

            // Task.Delay and FrozenImageLoader in production; in tests the sequence is handed
            // something that records what it was asked to wait for and returns immediately.
            selectedGameSequence = new SelectedGameSequence(
                this,
                timings,
                attractModeService,
                CaptureSelectedGame,
                FrozenImageLoader.LoadAsync,
                Task.Delay,
                () => mainWindowViewModel?.IsPlayingGame == true);

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
        public bool CanPlayVideo =>
            !disableVideos
            && !videoAbandonedForSession
            && !TypingInSearch
            && !string.IsNullOrWhiteSpace(activeVideoPath);

        /// <summary>
        /// Whether the user is typing rather than looking at results.
        ///
        /// The search results are an ordinary list, so moving through them settles and plays a
        /// video exactly as browsing does - which is what we want once the user is in the row.
        /// While they are still on the keyboard the selection changes on every keystroke, and a
        /// video starting up behind the keyboard on each one is noise rather than preview. The
        /// artwork still follows the selection either way; only the video waits.
        /// </summary>
        private bool TypingInSearch =>
            mainWindowViewModel?.IsDisplayingSearch == true
            && mainWindowViewModel.Search?.IsOnKeyboard == true;

        /// <summary>
        /// Hands a file to the player. Close() first: MediaElement is a wrapper over the legacy
        /// Windows media stack, which does not reliably release a file when Source is simply
        /// reassigned, and this used to happen once per game scrolled past.
        ///
        /// Assigning Source does not load anything. LoadedBehavior is Manual, so the file is not
        /// opened until Play() is called and MediaOpened arrives after that, not before it.
        /// </summary>
        public void OpenVideo(string videoPath)
        {
            // Recorded before the early return, because CanPlayVideo is asked about the game the
            // selection settled on rather than about the state of the player.
            activeVideoPath = videoPath;

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
        /// Carries out whatever <see cref="VideoFailurePolicy"/> decides a run of failures is
        /// worth. The decision is separate from the effect: what counts as enough failures is
        /// arithmetic and is unit tested, while closing the player is something only the view
        /// can do.
        /// </summary>
        private void ApplyFailurePolicy()
        {
            VideoFailureAction action =
                VideoFailurePolicy.Decide(videoMonitor.ConsecutiveFailures, videoAbandonedForSession);

            switch (action)
            {
                case VideoFailureAction.Abandon:
                    videoAbandonedForSession = true;
                    videoMonitor.RecoveryAbandoned();
                    break;

                case VideoFailureAction.Recover:
                    videoMonitor.RecoveryAttempted();

                    videoIsOpen = false;
                    videoPlayRequested = false;

                    Video_SelectedGame?.Close();

                    if (Video_SelectedGame != null)
                    {
                        Video_SelectedGame.Source = null;
                    }
                    break;
            }
        }

        private void DimBackground()
        {
            FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 0.25, timings.Dim.TotalMilliseconds);
            FadeFrameworkElementOpacity(Image_Selected_Background_Black, 1, timings.Dim.TotalMilliseconds);
            FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 0, timings.Dim.TotalMilliseconds);
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

        /// <summary>
        /// The view model's hook for "the selected game changed". Marshals to the UI thread and
        /// hands over to the sequence, which owns everything that follows.
        /// </summary>
        private void DoAnimateGameChange()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (mainWindowViewModel.IsDisplayingResults)
                    {
                        selectedGameSequence.GameChanged();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MainWindowView.xaml.cs.DoAnimateGameChange");
                }
            });
        }

        /// <summary>
        /// Dims the outgoing game. The change is acknowledged on screen straight away, before
        /// anything has been loaded - the fade back in waits for the settle (RULE-PRESENT-002).
        ///
        /// The opacities are set outright as well as animated: a finished WPF animation keeps
        /// ownership of the value it ended on, so a plain assignment on its own is silently
        /// ignored.
        /// </summary>
        public void DimForGameChange()
        {
            DimBackground();

            // dim logo image
            Image_Displayed_GameClearLogo.Opacity = 0.15;
            FadeFrameworkElementOpacity(Image_Displayed_GameClearLogo, 0.15, timings.Dim.TotalMilliseconds);

            // dim game title text all the way to 0
            // this is a backup for missing logo image
            TextBlock_Displayed_GameTitle.Opacity = 0;
            FadeFrameworkElementOpacity(TextBlock_Displayed_GameTitle, 0.00, timings.Dim.TotalMilliseconds);

            // dim game details
            Grid_SelectedGameDetails.Opacity = 0.15;
            FadeFrameworkElementOpacity(Grid_SelectedGameDetails, 0.15, timings.Dim.TotalMilliseconds);
        }

        /// <summary>
        /// The media paths and text for whichever game is selected right now. Reading these is
        /// free; decoding the images they point at is not, which is why only the paths are taken
        /// here and the decode waits for the settle.
        /// </summary>
        private SelectedGameMedia CaptureSelectedGame()
        {
            GameMatch settledGame = mainWindowViewModel?.CurrentGameList?.SelectedGame;
            GameFiles settledFiles = settledGame?.GameFiles;

            return new SelectedGameMedia
            {
                Background = settledFiles?.BackgroundImage,
                ClearLogo = settledFiles?.ClearLogo,
                PlayMode = settledFiles?.PlayModeImage,
                PlatformLogo = settledFiles?.PlatformClearLogoImage,
                GameBezel = settledFiles?.GameBezelImage,
                VideoPath = settledFiles?.VideoPath,

                Title = settledGame?.Game?.Title,
                MatchDescription = settledGame?.MatchDescription,
                ReleaseYear = settledGame?.ReleaseYear
            };
        }

        public void ShowGameDetails(DecodedGameMedia media)
        {
            // held so StopAndRestore can put the same game back on screen without the sequence
            shownMedia = media;

            if (Image_Active_BackgroundImage != null)
            {
                // outright, not a plain assignment - the fade that follows is skipped when the
                // opacity is already 1, and a held animation would keep it there
                SetOpacity(Image_Active_BackgroundImage, 0);
                Image_Active_BackgroundImage.Source = media?.Background;
            }

            // fade in the active clear logo
            Image_Displayed_GameClearLogo.Source = media?.ClearLogo;
            FadeFrameworkElementOpacity(Image_Displayed_GameClearLogo, 1, timings.DetailsFadeIn.TotalMilliseconds);

            // fade in the game title
            TextBlock_Displayed_GameTitle.Text = media?.Title;
            FadeFrameworkElementOpacity(TextBlock_Displayed_GameTitle, 1, timings.DetailsFadeIn.TotalMilliseconds);

            // fade in the active game details
            Image_Playmode.Source = media?.PlayMode;
            TextBlock_MatchPercentage.Text = media?.MatchDescription;
            TextBlock_ReleaseYear.Text = media?.ReleaseYear;
            Image_PlatformLogo.Source = media?.PlatformLogo;
            Image_Bezel.Source = media?.GameBezel;

            // fade in the game details
            FadeFrameworkElementOpacity(Grid_SelectedGameDetails, 1, timings.DetailsFadeIn.TotalMilliseconds);
        }

        public void FadeInBackground()
        {
            FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 1, timings.BackgroundFadeIn.TotalMilliseconds);
        }

        /// <summary>
        /// Ends the artwork change: the displayed layer takes the picture the active layer just
        /// faded in, at full brightness.
        ///
        /// Both layers are showing the same picture here, so the handover itself is invisible -
        /// what matters is the undim. The displayed layer has been at 0.25 since the game change
        /// began, so hiding the active layer without restoring it drops the background straight
        /// back to dim the moment the fade-in finishes.
        /// </summary>
        public void SettleBackground()
        {
            Image_Displayed_BackgroundImage.Source = Image_Active_BackgroundImage.Source;

            SetOpacity(Image_Displayed_BackgroundImage, 1);
            SetOpacity(Image_Active_BackgroundImage, 0);
        }

        public void PlayVideoAndFadeOutBackground()
        {
            PlayVideo(Video_SelectedGame);

            // fade background images while the video plays
            FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 0, timings.BackgroundFadeOut.TotalMilliseconds);
            FadeFrameworkElementOpacity(Image_Active_BackgroundImage, 0, timings.BackgroundFadeOut.TotalMilliseconds);
            FadeFrameworkElementOpacity(Image_Selected_Background_Black, 0, timings.BackgroundFadeOut.TotalMilliseconds);
        }

        public void SwapBackgroundLayers()
        {
            SwapBackgroundImages();
        }

        /// <summary>
        /// Stops the player and the check on whether playback started. Not a fade - just a halt,
        /// for when the selection moves or the screen is about to belong to something else.
        /// </summary>
        public void StopPlayback()
        {
            // the check must not outlive the playback attempt it was measuring
            playbackStartCheck?.Stop();

            PauseVideo(Video_SelectedGame);
        }

        /// <summary>
        /// Puts the selected game's artwork back on screen. Called when a game is launching or
        /// voice recognition starts, after the sequence has been stopped.
        /// </summary>
        public void StopAndRestore()
        {
            ShowGameDetails(shownMedia);
            FadeInBackgroundImages();
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
                    FadeFrameworkElementOpacity(Image_Selected_Background_Black, 1, timings.BackgroundFadeIn.TotalMilliseconds);
                    FadeFrameworkElementOpacity(Image_Displayed_BackgroundImage, 1, timings.BackgroundFadeIn.TotalMilliseconds);
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "FadeInBackgroundImages");
            }
        }



        /// <summary>
        /// The player has the file open, so its dimensions are known and the bezel can be chosen.
        ///
        /// `async void` because this is an event handler, which is the one place it is correct -
        /// but it means nothing observes a failure, hence the catch.
        /// </summary>
        private async void Video_SelectedGame_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                await ShowBezelForOpenedVideoAsync();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Video_SelectedGame_MediaOpened");
            }
        }

        private async Task ShowBezelForOpenedVideoAsync()
        {
            videoMonitor.OpenCompleted();

            // the file is genuinely loaded now, so this is the point to start timing whether
            // playback gets going
            videoIsOpen = true;

            if (videoPlayRequested)
            {
                StartPlaybackStartCheck();
            }

            // Which bezel, and which way round, is BezelService's decision - the view only
            // supplies what the player can tell it. The five-level chain, the widescreen cutoff
            // and the orientation used to be written out here; see RULE-MEDIA-020 to 027.
            GameMatch settledGame = mainWindowViewModel?.CurrentGameList?.SelectedGame;

            Uri gameBezelUri = BezelService.Instance.ResolveBezel(
                settledGame?.GameFiles?.GameBezelImage,
                settledGame?.Game?.Platform,
                Video_SelectedGame.NaturalVideoWidth,
                Video_SelectedGame.NaturalVideoHeight);

            // The mask depends only on *whether* there is a bezel, which is known now, so it is
            // set before the picture rather than after it (RULE-MEDIA-026).
            if (gameBezelUri == null)
            {
                Image_Bezel.Source = null;
                Image_Bezel.OpacityMask = null;
                Video_SelectedGame.OpacityMask = OpacityBrushHelper.Instance.OpacityBrush;
                return;
            }

            Image_Bezel.OpacityMask = OpacityBrushHelper.Instance.OpacityBrush;
            Video_SelectedGame.OpacityMask = null;

            // The default bezel can only be chosen once the video's dimensions are known, which
            // is here, after the artwork has already been put on screen - so this sets the source
            // itself rather than leaving it to ShowGameDetails, which has already run.
            //
            // Decoded off the UI thread like every other image. It used to be a synchronous
            // `new BitmapImage(uri)`, which was the last one in the product that was not - and a
            // bezel is a full-screen overlay, so it is not a small decode to do on the dispatcher.
            Uri videoThisBezelIsFor = Video_SelectedGame.Source;

            ImageSource bezelImage = await FrozenImageLoader.LoadAsync(gameBezelUri);

            // A new game can settle and open its own video while this one is decoding. Assigning
            // then would frame the new video with the old game's bezel.
            if (!Equals(Video_SelectedGame.Source, videoThisBezelIsFor))
            {
                return;
            }

            Image_Bezel.Source = bezelImage;
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
            // Marshalled here rather than inside the two calls: voice recognition raises
            // PresentationInterrupted from a thread pool thread, and both Stop and StopAndRestore
            // reach WPF elements. Every ISelectedGamePresenter member assumes the UI thread, so
            // the entry points are where getting onto it belongs.
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Stop is the sequence's: it turns off the idle countdown, halts the player
                    // and abandons the sequence in flight - otherwise that would carry on and
                    // start the video again a moment later. Putting the artwork back is this
                    // class's.
                    selectedGameSequence.Stop();

                    StopAndRestore();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "StopVideoAndAnimations");
                }
            });
        }
    }
}
