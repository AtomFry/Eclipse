using Eclipse.Models;
using Eclipse.Service;
using Newtonsoft.Json;
using System;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// VER-PRESENT-007 and VER-CONFIG-022, as far as they can be automated.
    ///
    /// The claim that matters is not that these timings are reasonable - it is that they are
    /// *the same numbers that were compiled in*, so an installation upgrading to this build
    /// behaves exactly as it did before the values became settings. Every expected value below
    /// is the literal that was in MainWindowView.xaml.cs, not a value read back from the code.
    /// </summary>
    public class SelectionTimingsTests
    {
        // What MainWindowView.xaml.cs held before B1:
        //   SettleDelayMilliseconds        = 1000
        //   BackgroundFadeInMilliseconds   =  500
        //   BackgroundFadeOutMilliseconds  = 1000
        //   the dim literal                =   25
        //   the details fade literal       =  500
        //   VideoDelayInMilliseconds       = 2000 (already a setting)
        private static void AssertOriginalTimings(SelectionTimings timings)
        {
            Assert.Equal(1000, timings.Settle.TotalMilliseconds);
            Assert.Equal(25, timings.Dim.TotalMilliseconds);
            Assert.Equal(500, timings.BackgroundFadeIn.TotalMilliseconds);
            Assert.Equal(1000, timings.BackgroundFadeOut.TotalMilliseconds);
            Assert.Equal(500, timings.DetailsFadeIn.TotalMilliseconds);
        }

        /// <summary>
        /// The path a first run takes: EclipseSettingsDataService builds its defaults by
        /// deserialising an empty object, so that Json.NET applies every [DefaultValue] rather
        /// than a hand-maintained list drifting out of step.
        /// </summary>
        [Fact]
        public void Defaults_are_the_constants_they_replaced()
        {
            EclipseSettings settings = JsonConvert.DeserializeObject<EclipseSettings>("{}");

            AssertOriginalTimings(new SelectionTimings(settings));
        }

        /// <summary>
        /// The upgrade case, and the reason this stage is safe: a settings file written before
        /// these settings existed has none of the new keys. Json.NET fills each one from its
        /// [DefaultValue], so the sequence runs at exactly the speed it used to.
        /// </summary>
        [Fact]
        public void A_settings_file_written_before_these_existed_keeps_the_old_behaviour()
        {
            string settingsFileFromAnEarlierBuild = @"{
                'IncludeBrokenGames': false,
                'DisableVideos': false,
                'VideoDelayInMilliseconds': 2000,
                'ScreensaverDelayInSeconds': 90,
                'ShowGameCountInList': true
            }".Replace('\'', '"');

            EclipseSettings settings =
                JsonConvert.DeserializeObject<EclipseSettings>(settingsFileFromAnEarlierBuild);

            AssertOriginalTimings(new SelectionTimings(settings));

            // and the one value that file did carry is honoured rather than defaulted over
            Assert.Equal(2000, settings.VideoDelayInMilliseconds);
        }

        [Fact]
        public void No_settings_at_all_falls_back_to_the_same_values()
        {
            AssertOriginalTimings(new SelectionTimings(null));
        }

        [Fact]
        public void Configured_values_are_used()
        {
            EclipseSettings settings = JsonConvert.DeserializeObject<EclipseSettings>("{}");
            settings.SelectionSettleMilliseconds = 250;
            settings.SelectionDimMilliseconds = 0;
            settings.SelectionBackgroundFadeInMilliseconds = 1200;

            SelectionTimings timings = new SelectionTimings(settings);

            Assert.Equal(250, timings.Settle.TotalMilliseconds);
            Assert.Equal(0, timings.Dim.TotalMilliseconds);
            Assert.Equal(1200, timings.BackgroundFadeIn.TotalMilliseconds);
        }

        /// <summary>
        /// Task.Delay throws on a negative duration, and these values reach Task.Delay. No
        /// slider can produce one - they all start at zero - but a hand-edited settings file
        /// can, and it should not take the browsing sequence down.
        /// </summary>
        [Fact]
        public void A_negative_duration_falls_back_to_its_default_rather_than_throwing()
        {
            EclipseSettings settings = JsonConvert.DeserializeObject<EclipseSettings>("{}");
            settings.SelectionSettleMilliseconds = -1;
            settings.SelectionDimMilliseconds = -500;
            settings.SelectionBackgroundFadeOutMilliseconds = -1000;

            SelectionTimings timings = new SelectionTimings(settings);

            Assert.Equal(1000, timings.Settle.TotalMilliseconds);
            Assert.Equal(25, timings.Dim.TotalMilliseconds);
            Assert.Equal(1000, timings.BackgroundFadeOut.TotalMilliseconds);
        }

        /// <summary>
        /// The video delay is the exception to the clamp above: zero or less is a supported
        /// configuration meaning "no pause before the video" (RULE-MEDIA-031), so a negative
        /// has to reach the caller that tests for it instead of being replaced by the default.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5000)]
        public void A_video_delay_of_zero_or_less_means_no_pause(int configured)
        {
            EclipseSettings settings = JsonConvert.DeserializeObject<EclipseSettings>("{}");
            settings.VideoDelayInMilliseconds = configured;

            SelectionTimings timings = new SelectionTimings(settings);

            Assert.False(timings.HasVideoPause);
            Assert.True(timings.VideoDelay <= TimeSpan.Zero);
        }

        [Fact]
        public void A_video_delay_above_zero_gives_the_artwork_its_moment()
        {
            EclipseSettings settings = JsonConvert.DeserializeObject<EclipseSettings>("{}");
            settings.VideoDelayInMilliseconds = 2000;

            SelectionTimings timings = new SelectionTimings(settings);

            Assert.True(timings.HasVideoPause);
            Assert.Equal(2000, timings.VideoDelay.TotalMilliseconds);
        }
    }
}
