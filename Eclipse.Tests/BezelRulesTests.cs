using Eclipse.Models;
using Eclipse.Service;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// VER-MEDIA-012, the part that can be automated. Which bezel a video gets, and which way
    /// round, used to be decided in the view and could only be checked by watching a game play.
    ///
    /// The cutoff is asserted either side of 1.7 rather than only at obvious values, because
    /// the comparison is a float ratio widened to a double - tidying it into double arithmetic
    /// would move the borderline cases, and these are what would catch that.
    /// </summary>
    public class BezelRulesTests
    {
        [Fact]
        public void A_game_bezel_wins_outright()
        {
            BezelSelection selection = BezelRules.Choose(true, 1920, 1080);

            Assert.Equal(BezelSource.GameSpecific, selection.Source);
        }

        /// <summary>
        /// The correction to RULE-MEDIA-024. The widescreen cutoff never applied to a bezel
        /// chosen for the game itself - in the code this replaced, the ratio test sat inside the
        /// "no game bezel" branch, so a 16:9 video with its own bezel was framed with it.
        /// </summary>
        [Fact]
        public void A_game_bezel_is_used_even_for_widescreen_video()
        {
            BezelSelection selection = BezelRules.Choose(true, 3840, 1080);

            Assert.Equal(BezelSource.GameSpecific, selection.Source);
        }

        [Fact]
        public void A_game_bezel_is_used_even_before_the_video_dimensions_are_known()
        {
            BezelSelection selection = BezelRules.Choose(true, 0, 0);

            Assert.Equal(BezelSource.GameSpecific, selection.Source);
        }

        [Theory]
        [InlineData(1920, 1080)]  // 16:9
        [InlineData(1280, 720)]
        [InlineData(3840, 1080)]  // ultrawide
        public void Widescreen_video_with_no_game_bezel_gets_no_bezel(int width, int height)
        {
            BezelSelection selection = BezelRules.Choose(false, width, height);

            Assert.Equal(BezelSource.None, selection.Source);
        }

        [Theory]
        [InlineData(4, 3)]        // 1.333
        [InlineData(320, 240)]
        [InlineData(1600, 1000)]  // 1.6
        public void Narrower_video_gets_a_default_bezel(int width, int height)
        {
            BezelSelection selection = BezelRules.Choose(false, width, height);

            Assert.Equal(BezelSource.Default, selection.Source);
        }

        [Fact]
        public void The_cutoff_sits_between_1_69_and_1_70()
        {
            // 169:100 is under the cutoff and is framed
            Assert.Equal(BezelSource.Default, BezelRules.Choose(false, 169, 100).Source);

            // 17:10 is exactly the cutoff, and the cutoff is exclusive - no bezel
            Assert.Equal(BezelSource.None, BezelRules.Choose(false, 17, 10).Source);

            // and anything wider
            Assert.Equal(BezelSource.None, BezelRules.Choose(false, 171, 100).Source);
        }

        /// <summary>
        /// Nothing is known about the video yet, so there is no orientation to pick and no ratio
        /// to test. Guarding on the height alone is what the original did.
        /// </summary>
        [Fact]
        public void An_unknown_video_height_gets_no_bezel()
        {
            Assert.Equal(BezelSource.None, BezelRules.Choose(false, 1920, 0).Source);
            Assert.Equal(BezelSource.None, BezelRules.Choose(false, 0, 0).Source);
        }

        [Fact]
        public void Taller_than_wide_gets_the_vertical_bezel()
        {
            BezelSelection selection = BezelRules.Choose(false, 600, 800);

            Assert.Equal(BezelSource.Default, selection.Source);
            Assert.Equal(BezelOrientation.Vertical, selection.Orientation);
        }

        [Fact]
        public void Wider_than_tall_gets_the_horizontal_bezel()
        {
            BezelSelection selection = BezelRules.Choose(false, 800, 600);

            Assert.Equal(BezelSource.Default, selection.Source);
            Assert.Equal(BezelOrientation.Horizontal, selection.Orientation);
        }

        [Fact]
        public void A_square_video_counts_as_horizontal()
        {
            BezelSelection selection = BezelRules.Choose(false, 700, 700);

            Assert.Equal(BezelSource.Default, selection.Source);
            Assert.Equal(BezelOrientation.Horizontal, selection.Orientation);
        }
    }
}
