using Eclipse.Models;
using System;

namespace Eclipse.Service
{
    /// <summary>
    /// Every duration in the selected-game sequence, read from settings and exposed as
    /// TimeSpans so nothing else has to convert milliseconds or reach for the settings
    /// provider. These used to be three constants and eleven literals in
    /// <c>MainWindowView.xaml.cs</c>.
    ///
    /// The sequence for one game change:
    ///
    ///   dim the outgoing game (Dim)
    ///   -> wait for the selection to stop moving (Settle)
    ///   -> load the media off the UI thread, open the video file
    ///   -> fade the details in (DetailsFadeIn) and the background in (BackgroundFadeIn)
    ///   -> wait (VideoDelay)
    ///   -> play the video, fading the background out behind it (BackgroundFadeOut)
    ///
    /// This is the same shape as <see cref="AttractModeTimings"/>, deliberately - the two
    /// sequences are siblings and should read like it.
    /// </summary>
    public sealed class SelectionTimings
    {
        public SelectionTimings(EclipseSettings eclipseSettings)
        {
            Settle = FromMilliseconds(eclipseSettings?.SelectionSettleMilliseconds, 1000);
            Dim = FromMilliseconds(eclipseSettings?.SelectionDimMilliseconds, 25);
            BackgroundFadeIn = FromMilliseconds(eclipseSettings?.SelectionBackgroundFadeInMilliseconds, 500);
            BackgroundFadeOut = FromMilliseconds(eclipseSettings?.SelectionBackgroundFadeOutMilliseconds, 1000);
            DetailsFadeIn = FromMilliseconds(eclipseSettings?.SelectionDetailsFadeInMilliseconds, 500);

            // Not clamped, unlike the rest. Zero or less means "no pause between the artwork
            // and the video", which is a supported configuration (RULE-MEDIA-031) - so a
            // negative value has to survive to the caller that tests for it rather than being
            // replaced by the default.
            VideoDelay = TimeSpan.FromMilliseconds(eclipseSettings?.VideoDelayInMilliseconds ?? 0);
        }

        /// <summary>How long the selection must be still before its media is loaded and shown.</summary>
        public TimeSpan Settle { get; }

        /// <summary>How quickly the outgoing game dims when the selection moves.</summary>
        public TimeSpan Dim { get; }

        /// <summary>
        /// How long the background artwork takes to fade in - when a new game settles, and when
        /// it returns after a video preview ends.
        /// </summary>
        public TimeSpan BackgroundFadeIn { get; }

        /// <summary>How long the background takes to fade out behind a video preview.</summary>
        public TimeSpan BackgroundFadeOut { get; }

        /// <summary>How long the clear logo, title and game details take to fade in.</summary>
        public TimeSpan DetailsFadeIn { get; }

        /// <summary>
        /// How long the new game's artwork holds the screen before the video takes over. Zero
        /// or less means the video starts as soon as the media is loaded.
        /// </summary>
        public TimeSpan VideoDelay { get; }

        /// <summary>Whether the artwork gets a moment on screen before the video.</summary>
        public bool HasVideoPause => VideoDelay > TimeSpan.Zero;

        private static TimeSpan FromMilliseconds(int? milliseconds, int fallback)
        {
            // A negative duration is not merely wrong-looking here: Task.Delay throws on one,
            // and these values reach Task.Delay. The settings window cannot produce a negative
            // - every slider starts at zero - but a hand-edited settings file can.
            int value = milliseconds.GetValueOrDefault(fallback);
            return TimeSpan.FromMilliseconds(value >= 0 ? value : fallback);
        }

        /// <summary>Timings from the current Eclipse settings.</summary>
        public static SelectionTimings Current =>
            new SelectionTimings(EclipseSettingsDataProvider.Instance?.EclipseSettings);
    }
}
