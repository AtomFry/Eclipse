using Eclipse.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eclipse.Service
{
    // The screen saver's slideshow, as one readable sequence
    public sealed class AttractModeSlideshow
    {
        private readonly IAttractModePresenter presenter;
        private readonly AttractModeTimings timings;
        private readonly Func<bool> canContinue;

        private CancellationTokenSource cancellationTokenSource;

        // The pan alternates direction each slide
        private bool slideLeft = true;

        /// <param name="canContinue">
        /// Checked before each slide - false stops the slideshow, e.g. because a game started.
        /// </param>
        public AttractModeSlideshow(IAttractModePresenter presenter, AttractModeTimings timings, Func<bool> canContinue)
        {
            this.presenter = presenter;
            this.timings = timings;
            this.canContinue = canContinue;
        }

        public void Start()
        {
            Stop();

            cancellationTokenSource = new CancellationTokenSource();

            // deliberately not awaited - this runs until it is cancelled
            _ = RunAsync(cancellationTokenSource.Token);
        }

        public void Stop()
        {
            CancellationTokenSource source = cancellationTokenSource;
            cancellationTokenSource = null;

            if (source != null)
            {
                source.Cancel();
                source.Dispose();
            }
        }

        // One slide, at the default timings:
        //
        //   t-4s  screen is black
        //   t+0   background fades in over 3s and a 17s pan begins
        //   t+4s  clear logo fades in over 1.5s
        //   t+15s background and logo fade out
        //   t+19s the next slide begins
        //
        // The pan deliberately outlasts the slide, so the image never visibly stops moving
        // before it fades.
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                presenter.FadeToBlack();

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(timings.DelayBetweenImages, cancellationToken);

                    if (!canContinue())
                    {
                        return;
                    }

                    slideLeft = !slideLeft;
                    presenter.FadeInAndSlideBackground(slideLeft);

                    await Task.Delay(timings.LogoDelay, cancellationToken);
                    presenter.FadeInLogo();

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
        }
    }
}
