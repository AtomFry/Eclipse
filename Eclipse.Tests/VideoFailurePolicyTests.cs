using Eclipse.Service;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// The escalation this pins is the whole of it: do nothing, reopen the player, or give up
    /// for the session. It was three branches inside a view that could only be exercised by
    /// making video playback fail repeatedly on a real machine - which is exactly the failure
    /// that takes hours to reproduce and was the reason the code exists.
    ///
    /// The counts are enumerated either side of both thresholds rather than sampled, because
    /// off-by-one is the only interesting way this can be wrong.
    /// </summary>
    public class VideoFailurePolicyTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Below_the_recovery_threshold_nothing_happens(int consecutiveFailures)
        {
            Assert.Equal(
                VideoFailureAction.None,
                VideoFailurePolicy.Decide(consecutiveFailures, false));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(9)]
        public void From_the_recovery_threshold_up_to_the_abandon_threshold_the_player_is_reopened(int consecutiveFailures)
        {
            Assert.Equal(
                VideoFailureAction.Recover,
                VideoFailurePolicy.Decide(consecutiveFailures, false));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(500)]
        public void At_the_abandon_threshold_previews_are_given_up_for_the_session(int consecutiveFailures)
        {
            Assert.Equal(
                VideoFailureAction.Abandon,
                VideoFailurePolicy.Decide(consecutiveFailures, false));
        }

        /// <summary>
        /// There is no level beyond abandoned, and re-reporting it on every subsequent game
        /// would bury the one log line that says when it happened.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(999)]
        public void Once_abandoned_nothing_further_is_decided(int consecutiveFailures)
        {
            Assert.Equal(
                VideoFailureAction.None,
                VideoFailurePolicy.Decide(consecutiveFailures, true));
        }

        /// <summary>
        /// The counter cannot go negative - VideoPlaybackMonitor only increments it and resets
        /// it to zero - but a policy that escalated on one would be a nasty way to find out.
        /// </summary>
        [Fact]
        public void A_negative_count_is_treated_as_no_failures()
        {
            Assert.Equal(
                VideoFailureAction.None,
                VideoFailurePolicy.Decide(-1, false));
        }

        /// <summary>
        /// The thresholds are part of the contract, not incidental. If either moves, the two
        /// boundary tests above should have moved with it deliberately.
        /// </summary>
        [Fact]
        public void The_thresholds_are_the_values_that_were_compiled_into_the_view()
        {
            Assert.Equal(3, VideoFailurePolicy.RecoveryFailureThreshold);
            Assert.Equal(10, VideoFailurePolicy.AbandonFailureThreshold);
        }
    }
}
