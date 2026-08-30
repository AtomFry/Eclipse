using Eclipse.Models;
using Eclipse.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// VER-PRESENT-008. The order of the selected-game sequence, and the durations it waits.
    ///
    /// `presentation.md` records this as the highest-value verification gap: "the idle-delay and
    /// fade sequence has no written specification anywhere except the code, and it is the
    /// behaviour most likely to be altered accidentally". This is that specification, in a form
    /// that fails when it stops being true.
    ///
    /// The waits are injected rather than real, so the whole suite runs in milliseconds. What is
    /// asserted is the sequence of calls and the durations it *asked* for - not that WPF honours
    /// them, which only a person watching can tell.
    /// </summary>
    public class SelectedGameSequenceTests
    {
        private const string AVideo = @"C:\videos\game.mp4";

        /// <summary>Records what the sequence asked the screen to do, in order.</summary>
        private sealed class RecordingPresenter : ISelectedGamePresenter
        {
            public List<string> Calls { get; } = new List<string>();

            /// <summary>What OpenVideo was handed, and whether a video is available to play.</summary>
            public string OpenedVideoPath { get; private set; }
            public bool VideosAvailable { get; set; } = true;

            public DecodedGameMedia Shown { get; private set; }

            public bool CanPlayVideo => VideosAvailable && !string.IsNullOrWhiteSpace(OpenedVideoPath);

            public void DimForGameChange() => Calls.Add(nameof(DimForGameChange));
            public void StopPlayback() => Calls.Add(nameof(StopPlayback));

            public void OpenVideo(string videoPath)
            {
                OpenedVideoPath = videoPath;
                Calls.Add(nameof(OpenVideo));
            }

            public void ShowGameDetails(DecodedGameMedia media)
            {
                Shown = media;
                Calls.Add(nameof(ShowGameDetails));
            }

            public void FadeInBackground() => Calls.Add(nameof(FadeInBackground));
            public void SettleBackground() => Calls.Add(nameof(SettleBackground));
            public void PlayVideoAndFadeOutBackground() => Calls.Add(nameof(PlayVideoAndFadeOutBackground));
            public void SwapBackgroundLayers() => Calls.Add(nameof(SwapBackgroundLayers));
            public void StopAndRestore() => Calls.Add(nameof(StopAndRestore));
        }

        private sealed class RecordingAttractMode : IAttractModeTimer
        {
            public List<string> Calls { get; } = new List<string>();

            public void StopAttractMode() => Calls.Add(nameof(StopAttractMode));
            public void RestartAttractMode() => Calls.Add(nameof(RestartAttractMode));
        }

        private sealed class Harness
        {
            public RecordingPresenter Presenter { get; } = new RecordingPresenter();
            public RecordingAttractMode AttractMode { get; } = new RecordingAttractMode();
            public List<TimeSpan> Waits { get; } = new List<TimeSpan>();
            public SelectedGameMedia Captured { get; set; }
            public bool GameIsRunning { get; set; }

            /// <summary>Released to let a wait complete, when a test needs to interrupt mid-flight.</summary>
            public TaskCompletionSource<bool> HoldNextWait { get; set; }

            /// <summary>The same, for an image decode - the other place a selection can move on.</summary>
            public TaskCompletionSource<bool> HoldNextLoad { get; set; }

            public SelectedGameSequence Build(EclipseSettings settings = null)
            {
                return new SelectedGameSequence(
                    Presenter,
                    new SelectionTimings(settings ?? Newtonsoft.Json.JsonConvert.DeserializeObject<EclipseSettings>("{}")),
                    AttractMode,
                    () => Captured,
                    uri =>
                    {
                        TaskCompletionSource<bool> hold = HoldNextLoad;
                        if (hold != null)
                        {
                            HoldNextLoad = null;
                            return hold.Task.ContinueWith(_ => (ImageSource)null);
                        }

                        return Task.FromResult<ImageSource>(null);
                    },
                    (duration, token) =>
                    {
                        Waits.Add(duration);

                        TaskCompletionSource<bool> hold = HoldNextWait;
                        if (hold != null)
                        {
                            HoldNextWait = null;
                            return hold.Task.ContinueWith(_ => token.ThrowIfCancellationRequested());
                        }

                        token.ThrowIfCancellationRequested();
                        return Task.CompletedTask;
                    },
                    () => GameIsRunning);
            }
        }

        private static SelectedGameMedia AGameWith(string videoPath)
        {
            return new SelectedGameMedia
            {
                Background = new Uri(@"C:\art\background.jpg"),
                ClearLogo = new Uri(@"C:\art\logo.png"),
                VideoPath = videoPath,
                Title = "Some Game",
                ReleaseYear = "1991"
            };
        }

        /// <summary>
        /// Waits for the sequence, which is deliberately not awaited by GameChanged.
        ///
        /// Real delays rather than Task.Yield: yielding only requeues this test on the thread
        /// pool, it does not give a released continuation chain elsewhere a chance to run. A
        /// yield-based version of this let a mutation survive - the sequence had genuinely
        /// misbehaved, and the assertion simply ran before the misbehaviour arrived.
        /// </summary>
        private static async Task Settle()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// Gives <paramref name="condition"/> a bounded chance to become true. Used for asserting
        /// something must *not* happen: the wait has to be long enough that it would have
        /// happened by now, or the assertion proves nothing.
        /// </summary>
        private static async Task GiveItAChanceTo(Func<bool> condition)
        {
            for (int attempt = 0; attempt < 100 && !condition(); attempt++)
            {
                await Task.Delay(5);
            }
        }

        [Fact]
        public async Task An_ordinary_game_change_runs_the_whole_sequence_in_order()
        {
            Harness harness = new Harness { Captured = AGameWith(AVideo) };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.Equal(
                new[]
                {
                    nameof(ISelectedGamePresenter.StopPlayback),
                    nameof(ISelectedGamePresenter.DimForGameChange),
                    nameof(ISelectedGamePresenter.OpenVideo),
                    nameof(ISelectedGamePresenter.ShowGameDetails),
                    nameof(ISelectedGamePresenter.FadeInBackground),
                    nameof(ISelectedGamePresenter.SettleBackground),
                    nameof(ISelectedGamePresenter.PlayVideoAndFadeOutBackground),
                    nameof(ISelectedGamePresenter.SwapBackgroundLayers)
                },
                harness.Presenter.Calls);
        }

        [Fact]
        public async Task The_durations_are_the_settle_the_fade_in_the_video_delay_and_the_fade_out()
        {
            Harness harness = new Harness { Captured = AGameWith(AVideo) };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.Equal(
                new[]
                {
                    TimeSpan.FromMilliseconds(1000),  // settle
                    TimeSpan.FromMilliseconds(500),   // background fade in
                    TimeSpan.FromMilliseconds(2000),  // video delay
                    TimeSpan.FromMilliseconds(1000)   // background fade out
                },
                harness.Waits);
        }

        /// <summary>
        /// The debounce. Nothing is loaded or shown until the selection has been still, so
        /// holding a direction through fifty games costs one pass rather than fifty.
        /// </summary>
        [Fact]
        public async Task Nothing_is_shown_before_the_settle_elapses()
        {
            Harness harness = new Harness
            {
                Captured = AGameWith(AVideo),
                HoldNextWait = new TaskCompletionSource<bool>()
            };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            // the dim has happened - that is the instant feedback - but nothing else
            Assert.Equal(
                new[]
                {
                    nameof(ISelectedGamePresenter.StopPlayback),
                    nameof(ISelectedGamePresenter.DimForGameChange)
                },
                harness.Presenter.Calls);
        }

        [Fact]
        public async Task A_newer_selection_cancels_the_one_in_flight()
        {
            TaskCompletionSource<bool> heldSettle = new TaskCompletionSource<bool>();
            Harness harness = new Harness
            {
                Captured = AGameWith(AVideo),
                HoldNextWait = heldSettle
            };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            // a second game change arrives while the first is still waiting to settle
            sequence.GameChanged();

            // release the first sequence's settle - it must find itself cancelled
            heldSettle.SetResult(true);
            await Settle();

            // the first run never got past its dim, so nothing was shown for the game the user
            // scrolled straight past
            Assert.Equal(1, harness.Presenter.Calls.Count(call => call == nameof(ISelectedGamePresenter.ShowGameDetails)));
        }

        /// <summary>
        /// The selection can also move on *during the decode*, not only during a wait - and on a
        /// cold cache the decode is the slow part. Without the cancellation check between loading
        /// and showing, the scrolled-past game's artwork gets painted over the game the user has
        /// actually moved to.
        ///
        /// This test exists because a mutation that deleted that check survived the rest of the
        /// suite.
        /// </summary>
        [Fact]
        public async Task A_selection_that_moves_on_while_the_media_is_loading_is_not_shown()
        {
            TaskCompletionSource<bool> heldLoad = new TaskCompletionSource<bool>();
            Harness harness = new Harness
            {
                Captured = AGameWith(AVideo),
                HoldNextLoad = heldLoad
            };
            SelectedGameSequence sequence = harness.Build();

            // settles immediately, then blocks inside the decode
            sequence.GameChanged();
            await Settle();

            Assert.DoesNotContain(nameof(ISelectedGamePresenter.ShowGameDetails), harness.Presenter.Calls);

            // the user moves on while that decode is still running
            sequence.GameChanged();
            await Settle();

            // release the abandoned run's decode - its artwork must not reach the screen
            heldLoad.SetResult(true);

            // wait long enough that a second ShowGameDetails would have arrived if it were coming
            await GiveItAChanceTo(
                () => harness.Presenter.Calls.Count(call => call == nameof(ISelectedGamePresenter.ShowGameDetails)) > 1);

            Assert.Equal(
                1,
                harness.Presenter.Calls.Count(call => call == nameof(ISelectedGamePresenter.ShowGameDetails)));
        }

        /// <summary>RULE-MEDIA-031: a zero video delay skips the artwork's moment on screen.</summary>
        [Fact]
        public async Task A_zero_video_delay_skips_the_background_fade_in_entirely()
        {
            EclipseSettings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<EclipseSettings>("{}");
            settings.VideoDelayInMilliseconds = 0;

            Harness harness = new Harness { Captured = AGameWith(AVideo) };
            SelectedGameSequence sequence = harness.Build(settings);

            sequence.GameChanged();
            await Settle();

            Assert.DoesNotContain(nameof(ISelectedGamePresenter.FadeInBackground), harness.Presenter.Calls);
            Assert.DoesNotContain(nameof(ISelectedGamePresenter.SettleBackground), harness.Presenter.Calls);
            Assert.Contains(nameof(ISelectedGamePresenter.PlayVideoAndFadeOutBackground), harness.Presenter.Calls);

            // settle, then the fade out - no fade in and no video delay
            Assert.Equal(
                new[] { TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000) },
                harness.Waits);
        }

        /// <summary>
        /// The bug the current code comments describe: the idle countdown was stopped for a game
        /// that had no video, and only MediaEnded ever turned it back on - so the screen saver
        /// never started at all.
        /// </summary>
        [Fact]
        public async Task A_game_with_no_video_settles_the_background_and_leaves_the_screen_saver_armed()
        {
            Harness harness = new Harness { Captured = AGameWith(null) };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.Equal(
                new[]
                {
                    nameof(ISelectedGamePresenter.StopPlayback),
                    nameof(ISelectedGamePresenter.DimForGameChange),
                    nameof(ISelectedGamePresenter.OpenVideo),
                    nameof(ISelectedGamePresenter.ShowGameDetails),
                    nameof(ISelectedGamePresenter.FadeInBackground),
                    nameof(ISelectedGamePresenter.SettleBackground)
                },
                harness.Presenter.Calls);

            // armed on the way in, and never stopped, because no video took the screen
            Assert.Equal(new[] { nameof(IAttractModeTimer.RestartAttractMode) }, harness.AttractMode.Calls);
        }

        [Fact]
        public async Task Videos_disabled_shows_the_artwork_and_plays_nothing()
        {
            Harness harness = new Harness { Captured = AGameWith(AVideo) };
            harness.Presenter.VideosAvailable = false;

            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.Contains(nameof(ISelectedGamePresenter.ShowGameDetails), harness.Presenter.Calls);
            Assert.DoesNotContain(nameof(ISelectedGamePresenter.PlayVideoAndFadeOutBackground), harness.Presenter.Calls);
        }

        /// <summary>RULE-MEDIA-032: video never starts while a game is running.</summary>
        [Fact]
        public async Task No_video_starts_while_a_game_is_running()
        {
            Harness harness = new Harness
            {
                Captured = AGameWith(AVideo),
                GameIsRunning = true
            };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.DoesNotContain(nameof(ISelectedGamePresenter.PlayVideoAndFadeOutBackground), harness.Presenter.Calls);

            // and the countdown is handed back rather than left stopped
            Assert.Equal(nameof(IAttractModeTimer.RestartAttractMode), harness.AttractMode.Calls.Last());
        }

        /// <summary>RULE-MEDIA-033: starting a video stops attract mode.</summary>
        [Fact]
        public async Task Starting_a_video_stops_the_idle_countdown()
        {
            Harness harness = new Harness { Captured = AGameWith(AVideo) };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.Equal(
                new[]
                {
                    nameof(IAttractModeTimer.RestartAttractMode),  // re-armed on the game change
                    nameof(IAttractModeTimer.StopAttractMode)      // handed over to the video
                },
                harness.AttractMode.Calls);
        }

        [Fact]
        public async Task Stopping_halts_the_player_and_the_countdown_and_abandons_the_sequence()
        {
            Harness harness = new Harness
            {
                Captured = AGameWith(AVideo),
                HoldNextWait = new TaskCompletionSource<bool>()
            };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            harness.AttractMode.Calls.Clear();
            harness.Presenter.Calls.Clear();

            sequence.Stop();

            Assert.Equal(new[] { nameof(IAttractModeTimer.StopAttractMode) }, harness.AttractMode.Calls);
            Assert.Equal(new[] { nameof(ISelectedGamePresenter.StopPlayback) }, harness.Presenter.Calls);
        }

        [Fact]
        public async Task The_captured_text_reaches_the_screen()
        {
            Harness harness = new Harness { Captured = AGameWith(AVideo) };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.Equal("Some Game", harness.Presenter.Shown.Title);
            Assert.Equal("1991", harness.Presenter.Shown.ReleaseYear);
        }

        [Fact]
        public async Task A_selection_with_nothing_in_it_does_not_throw()
        {
            Harness harness = new Harness { Captured = null };
            SelectedGameSequence sequence = harness.Build();

            sequence.GameChanged();
            await Settle();

            Assert.Contains(nameof(ISelectedGamePresenter.ShowGameDetails), harness.Presenter.Calls);
            Assert.DoesNotContain(nameof(ISelectedGamePresenter.PlayVideoAndFadeOutBackground), harness.Presenter.Calls);
        }
    }
}
