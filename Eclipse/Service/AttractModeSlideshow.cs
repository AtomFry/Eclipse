using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Eclipse.Service
{
    // The screen saver's slideshow, as one readable sequence
    public sealed class AttractModeSlideshow
    {
        private readonly IAttractModePresenter presenter;
        private readonly AttractModeTimings timings;
        private readonly Func<GameMatch> selectGame;
        private readonly Func<bool> canContinue;

        private CancellationTokenSource cancellationTokenSource;

        // The pan alternates direction each slide
        private bool slideLeft = true;

        /// <param name="selectGame">Picks the game for the next slide. May return null.</param>
        /// <param name="canContinue">
        /// Checked before each slide - false stops the slideshow, e.g. because a game started.
        /// </param>
        public AttractModeSlideshow(
            IAttractModePresenter presenter,
            AttractModeTimings timings,
            Func<GameMatch> selectGame,
            Func<bool> canContinue)
        {
            this.presenter = presenter;
            this.timings = timings;
            this.selectGame = selectGame;
            this.canContinue = canContinue;
        }

        public void Start()
        {
            Stop();

            CancellationTokenSource source = new CancellationTokenSource();
            cancellationTokenSource = source;

            // deliberately not awaited - this runs until it is cancelled
            _ = RunAsync(source);
        }

        public void Stop()
        {
            // Cancel only. RunAsync owns the disposal: cancelling while it is between awaits
            // and disposing here would make its next Task.Delay throw ObjectDisposedException,
            // which would be logged as a real error on every ordinary exit.
            Interlocked.Exchange(ref cancellationTokenSource, null)?.Cancel();
        }

        // One slide, at the default timings:
        //
        //   t-4s  screen is black, while the next game's artwork is picked and decoded
        //   t+0   background fades in over 3s and a 17s pan begins
        //   t+4s  clear logo fades in over 1.5s
        //   t+15s background and logo fade out
        //   t+19s the next slide begins
        //
        // The pan deliberately outlasts the slide, so the image never visibly stops moving
        // before it fades.
        private async Task RunAsync(CancellationTokenSource source)
        {
            CancellationToken cancellationToken = source.Token;

            try
            {
                presenter.FadeToBlack();

                while (!cancellationToken.IsCancellationRequested)
                {
                    // The hold on black and the work to prepare the next slide run together.
                    // Decoding after the hold instead would add its cost to every slide, and
                    // the whole point of the hold is that there is nothing to look at anyway.
                    Task blackHold = Task.Delay(timings.DelayBetweenImages, cancellationToken);

                    GameMatch game = selectGame();
                    ImageSource background = await LoadBackgroundAsync(game);

                    await blackHold;

                    if (!canContinue())
                    {
                        return;
                    }

                    slideLeft = !slideLeft;
                    presenter.ShowBackground(background, slideLeft);

                    // and again: the logo decodes while it is waiting to appear
                    Task<ImageSource> logo = AttractModeImageLoader.LoadAsync(game?.GameFiles?.ClearLogo);

                    await Task.Delay(timings.LogoDelay, cancellationToken);
                    presenter.ShowLogo(await logo);

                    // the logo delay is part of the game's time on screen, so only the
                    // remainder is left to wait
                    await Task.Delay(timings.HoldAfterLogo, cancellationToken);
                    presenter.FadeOutBackgroundAndLogo();
                }
            }
            catch (OperationCanceledException)
            {
                // expected whenever the user leaves attract mode
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "run the attract mode slideshow");
            }
            finally
            {
                source.Dispose();
            }
        }

        // Media is hydrated lazily in the background and attract mode picks from the whole
        // library, so a game's Uris can still be null when it is chosen. Hydrating it here
        // means the slide shows the game's own artwork instead of the placeholder.
        private static async Task<ImageSource> LoadBackgroundAsync(GameMatch game)
        {
            if (game?.GameFiles != null)
            {
                await game.GameFiles.SetupFiles();
            }

            // GameFiles.SetupFiles sets IsSetup before it populates, so a game already being
            // hydrated elsewhere returns immediately with its Uris still null. Keep the
            // fallback: a placeholder background beats a slide with nothing on it.
            Uri backgroundUri = game?.GameFiles?.BackgroundImage;

            if (backgroundUri == null)
            {
                return await AttractModeImageLoader.LoadAsync(ResourceImages.DefaultBackground);
            }

            // a game whose artwork is missing or corrupt falls back the same way
            return await AttractModeImageLoader.LoadAsync(backgroundUri)
                   ?? await AttractModeImageLoader.LoadAsync(ResourceImages.DefaultBackground);
        }
    }
}
