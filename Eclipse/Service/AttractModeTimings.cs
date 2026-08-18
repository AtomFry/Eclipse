using Eclipse.Models;
using System;

namespace Eclipse.Service
{
    // Every duration in the screen saver slideshow, read from settings and exposed as
    // TimeSpans so nothing else has to convert milliseconds or reach for the settings
    // provider. These used to be literals scattered through AttractModeState and
    // MainWindowView.
    //
    // The sequence for one game:
    //
    //   wait (DelayBetweenImages) with the screen fading to black
    //   -> pick a game, fade the background in (BackgroundFadeIn) and start the pan (Pan)
    //   -> wait (LogoDelay), fade the logo in (LogoFadeIn)
    //   -> wait until GameDuration has elapsed since the pick
    //   -> fade background (BackgroundFadeOut) and logo (LogoFadeOut) out, repeat
    //
    // The pan deliberately runs longer than the game is displayed, so it never visibly
    // stops before the image fades.
    public sealed class AttractModeTimings
    {
        public AttractModeTimings(EclipseSettings eclipseSettings)
        {
            FadeIn = FromMilliseconds(eclipseSettings?.ScreensaverFadeInMilliseconds, 1000);
            DelayBetweenImages = FromMilliseconds(eclipseSettings?.ScreensaverDelayBetweenImagesMilliseconds, 4000);
            BackgroundFadeIn = FromMilliseconds(eclipseSettings?.ScreensaverBackgroundFadeInMilliseconds, 3000);
            Pan = FromMilliseconds(eclipseSettings?.ScreensaverPanMilliseconds, 17000);
            LogoDelay = FromMilliseconds(eclipseSettings?.ScreensaverLogoDelayMilliseconds, 4000);
            LogoFadeIn = FromMilliseconds(eclipseSettings?.ScreensaverLogoFadeInMilliseconds, 1500);
            GameDuration = FromMilliseconds(eclipseSettings?.ScreensaverGameDurationMilliseconds, 15000);
            BackgroundFadeOut = FromMilliseconds(eclipseSettings?.ScreensaverBackgroundFadeOutMilliseconds, 3000);
            LogoFadeOut = FromMilliseconds(eclipseSettings?.ScreensaverLogoFadeOutMilliseconds, 500);
            ExitFade = FromMilliseconds(eclipseSettings?.ScreensaverExitFadeMilliseconds, 500);
        }

        /// <summary>How long the screen saver takes to fade in when it starts.</summary>
        public TimeSpan FadeIn { get; }

        /// <summary>Time from one image starting to fade out until the next starts to fade in.</summary>
        public TimeSpan DelayBetweenImages { get; }

        /// <summary>How long a game's background image takes to fade in.</summary>
        public TimeSpan BackgroundFadeIn { get; }

        /// <summary>How long the background image takes to pan across the screen.</summary>
        public TimeSpan Pan { get; }

        /// <summary>How long after the background appears before the logo fades in.</summary>
        public TimeSpan LogoDelay { get; }

        /// <summary>How long the game's logo takes to fade in.</summary>
        public TimeSpan LogoFadeIn { get; }

        /// <summary>How long each game is displayed before it fades out.</summary>
        public TimeSpan GameDuration { get; }

        /// <summary>How long the background image takes to fade out.</summary>
        public TimeSpan BackgroundFadeOut { get; }

        /// <summary>How long the game's logo takes to fade out.</summary>
        public TimeSpan LogoFadeOut { get; }

        /// <summary>How quickly the screen saver disappears when the user presses a key.</summary>
        public TimeSpan ExitFade { get; }

        /// <summary>
        /// How long to wait after the logo has faded in before fading the game out. The logo
        /// delay is part of the game's display time, so this is the remainder - clamped at
        /// zero in case someone sets a logo delay longer than the game duration.
        /// </summary>
        public TimeSpan HoldAfterLogo
        {
            get
            {
                TimeSpan remaining = GameDuration - LogoDelay;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        private static TimeSpan FromMilliseconds(int? milliseconds, int fallback)
        {
            // a negative or absent value would produce a backwards animation, so fall back
            int value = milliseconds.GetValueOrDefault(fallback);
            return TimeSpan.FromMilliseconds(value >= 0 ? value : fallback);
        }

        /// <summary>Timings from the current Eclipse settings.</summary>
        public static AttractModeTimings Current =>
            new AttractModeTimings(EclipseSettingsDataProvider.Instance?.EclipseSettings);
    }
}
