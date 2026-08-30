using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Eclipse.Service
{
    /// <summary>
    /// Everything that happens once a game is selected, as one sequence: dim what is on screen,
    /// wait for the selection to settle, load the media, show the artwork, then hand the screen
    /// over to the video. A new selection cancels the sequence in flight.
    ///
    /// The shape of this - one cancellable sequence rather than chained callbacks - is not new
    /// and is not being changed here. It replaced two thread-pool timers whose steps were
    /// chained by animation Completed callbacks; those could not be cancelled, so a step could
    /// run for a game that was no longer selected, and a fade that was skipped for already
    /// being at its target opacity meant a callback that never fired and a video that never
    /// played. What this stage changes is only *where it lives*: out of the view, behind
    /// <see cref="ISelectedGamePresenter"/>, where the ordering can be asserted.
    ///
    /// **Nothing here uses ConfigureAwait(false), and that is load-bearing.** Every step after
    /// an await touches the presenter, which touches WPF elements. The sequence is started on
    /// the UI thread, so each await resumes there. Adding ConfigureAwait(false) anywhere below
    /// would move a presenter call onto a thread pool thread.
    /// </summary>
    public sealed class SelectedGameSequence
    {
        private readonly ISelectedGamePresenter presenter;
        private readonly SelectionTimings timings;
        private readonly IAttractModeTimer attractMode;
        private readonly Func<SelectedGameMedia> captureSelection;
        private readonly Func<Uri, Task<ImageSource>> loadImage;
        private readonly Func<TimeSpan, CancellationToken, Task> delay;
        private readonly Func<bool> isGameRunning;

        private CancellationTokenSource selectionCancellation;

        /// <param name="captureSelection">
        /// Reads the currently selected game's media paths and text. Called on the UI thread,
        /// synchronously, at the moment the selection changes.
        /// </param>
        /// <param name="loadImage">Decodes an image off the UI thread; null Uri gives null.</param>
        /// <param name="delay">
        /// Waits. <c>Task.Delay</c> in production; in tests, something that records the duration
        /// and returns immediately - which is what makes the ordering and the durations
        /// assertable in a suite that has to run in under a second.
        /// </param>
        /// <param name="isGameRunning">Whether a game is being played right now.</param>
        public SelectedGameSequence(
            ISelectedGamePresenter presenter,
            SelectionTimings timings,
            IAttractModeTimer attractMode,
            Func<SelectedGameMedia> captureSelection,
            Func<Uri, Task<ImageSource>> loadImage,
            Func<TimeSpan, CancellationToken, Task> delay,
            Func<bool> isGameRunning)
        {
            this.presenter = presenter;
            this.timings = timings;
            this.attractMode = attractMode;
            this.captureSelection = captureSelection;
            this.loadImage = loadImage;
            this.delay = delay;
            this.isGameRunning = isGameRunning;
        }

        /// <summary>
        /// The selected game changed. Runs its first steps synchronously so the dim is on screen
        /// before anything waits, then hands over to the sequence.
        ///
        /// Must be called on the UI thread.
        /// </summary>
        public void GameChanged()
        {
            try
            {
                CancelInFlight();
                presenter.StopPlayback();

                // Re-arm the idle countdown. Changing the selected game is ordinary browsing, so
                // the screen saver should still be waiting to start - it used to be left stopped
                // here and only a video's MediaEnded turned it back on, so a game with no video
                // meant the screen saver never started at all. RestartAttractMode stops before it
                // starts, which is why there is no separate stop above.
                attractMode.RestartAttractMode();

                presenter.DimForGameChange();

                SelectedGameMedia captured = captureSelection();

                CancellationTokenSource cancellation = new CancellationTokenSource();
                Interlocked.Exchange(ref selectionCancellation, cancellation);

                // deliberately not awaited - this runs until it finishes or is cancelled
                _ = RunAsync(captured, cancellation);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "start the selected game sequence");
            }
        }

        /// <summary>
        /// Abandon the sequence in flight and stop the player. Called when a game is launching or
        /// voice recognition starts - the screen is about to belong to something else.
        /// </summary>
        public void Stop()
        {
            attractMode.StopAttractMode();
            presenter.StopPlayback();
            CancelInFlight();
        }

        private void CancelInFlight()
        {
            // Cancel only. RunAsync owns the disposal: cancelling while it is between awaits and
            // disposing here would make its next wait throw ObjectDisposedException, which would
            // be logged as a real error on every ordinary game change.
            Interlocked.Exchange(ref selectionCancellation, null)?.Cancel();
        }

        private async Task RunAsync(SelectedGameMedia captured, CancellationTokenSource cancellation)
        {
            CancellationToken cancellationToken = cancellation.Token;

            try
            {
                // The debounce. Nothing is read from disk and nothing is shown until the
                // selection has been still, so holding a direction through fifty games costs one
                // pass of the work below rather than fifty.
                await delay(timings.Settle, cancellationToken);

                DecodedGameMedia decoded = await LoadAsync(captured);

                // the selection may have moved on while the files were being read, in which case
                // this artwork belongs to a game that is no longer on screen
                cancellationToken.ThrowIfCancellationRequested();

                // the selection settled here, so this is the first point worth opening a file
                presenter.OpenVideo(captured?.VideoPath);

                presenter.ShowGameDetails(decoded);

                if (presenter.CanPlayVideo && !timings.HasVideoPause)
                {
                    // no pause configured, so the artwork never gets its moment on screen
                    await PlayVideoAsync(cancellationToken);
                    return;
                }

                // fade the new artwork in over the outgoing game's dimmed artwork
                presenter.FadeInBackground();
                await delay(timings.BackgroundFadeIn, cancellationToken);

                presenter.SettleBackground();

                if (!presenter.CanPlayVideo)
                {
                    return;
                }

                await delay(timings.VideoDelay, cancellationToken);

                await PlayVideoAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // a newer selection took over, or a game is launching - either way this one is
                // no longer the game on screen and has nothing left to do
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "run the selected game sequence");
            }
            finally
            {
                Interlocked.CompareExchange(ref selectionCancellation, null, cancellation);
                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Hands the screen over to the video: the idle countdown stops, playback starts, and the
        /// background artwork fades out behind it.
        /// </summary>
        private async Task PlayVideoAsync(CancellationToken cancellationToken)
        {
            // Only hand the idle countdown over to the video if one is actually going to play.
            // Nothing but MediaEnded turns it back on, so stopping it when there is no video
            // left the screen saver switched off until the next keypress.
            if (!presenter.CanPlayVideo || isGameRunning())
            {
                attractMode.RestartAttractMode();
                return;
            }

            attractMode.StopAttractMode();

            presenter.PlayVideoAndFadeOutBackground();

            await delay(timings.BackgroundFadeOut, cancellationToken);

            presenter.SwapBackgroundLayers();
        }

        private async Task<DecodedGameMedia> LoadAsync(SelectedGameMedia captured)
        {
            // One at a time rather than in parallel: FrozenImageLoader gates decodes to two at
            // once across the whole plugin anyway, and starting five that immediately queue
            // behind each other buys nothing.
            return new DecodedGameMedia
            {
                Background = await loadImage(captured?.Background),
                ClearLogo = await loadImage(captured?.ClearLogo),
                PlayMode = await loadImage(captured?.PlayMode),
                PlatformLogo = await loadImage(captured?.PlatformLogo),
                GameBezel = await loadImage(captured?.GameBezel),

                Title = captured?.Title,
                MatchDescription = captured?.MatchDescription,
                ReleaseYear = captured?.ReleaseYear
            };
        }
    }
}
