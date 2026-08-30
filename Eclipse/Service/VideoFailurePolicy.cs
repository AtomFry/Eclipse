namespace Eclipse.Service
{
    /// <summary>What to do about a run of video previews that would not play.</summary>
    public enum VideoFailureAction
    {
        /// <summary>Nothing. One or two failures prove nothing on their own.</summary>
        None,

        /// <summary>Close the player and clear its source, so the next game starts clean.</summary>
        Recover,

        /// <summary>Stop attempting video previews for the rest of the session.</summary>
        Abandon
    }

    /// <summary>
    /// How many consecutive playback failures are worth acting on.
    ///
    /// The failure being handled: previews stop playing after a long session and never recover
    /// without restarting Big Box, while background artwork keeps working. Every failure is
    /// logged, because the log is the only witness to a bug that takes hours to appear - but
    /// nothing is done about a failure on its own. Only a *run* of them with no successful
    /// playback in between is evidence that the media stack itself is in trouble, and any
    /// successful playback resets the count (see <see cref="VideoPlaybackMonitor.PlayStarted"/>).
    ///
    /// These thresholds are deliberately not settings. They are diagnostics rather than
    /// presentation: nothing in the UI reports that recovery fired or that previews were
    /// abandoned, so their effect cannot be observed, and a wrong value silently disables video
    /// previews with no visible cause. See D3 in
    /// docs/plans/media-and-presentation-refactor.md.
    /// </summary>
    public static class VideoFailurePolicy
    {
        /// <summary>
        /// One missed playback check is not proof of anything. Three in a row with nothing
        /// playing in between is enough to try reopening the player.
        /// </summary>
        public const int RecoveryFailureThreshold = 3;

        /// <summary>Ten in a row means the media stack is not coming back this session.</summary>
        public const int AbandonFailureThreshold = 10;

        /// <param name="consecutiveFailures">Failures since the last successful playback.</param>
        /// <param name="alreadyAbandoned">Whether previews have already been given up on.</param>
        public static VideoFailureAction Decide(int consecutiveFailures, bool alreadyAbandoned)
        {
            // Nothing left to do once previews are off for the session - there is no deeper
            // level to escalate to, and re-reporting it on every subsequent game would bury the
            // one log line that says when it happened.
            if (alreadyAbandoned)
            {
                return VideoFailureAction.None;
            }

            if (consecutiveFailures >= AbandonFailureThreshold)
            {
                return VideoFailureAction.Abandon;
            }

            if (consecutiveFailures >= RecoveryFailureThreshold)
            {
                return VideoFailureAction.Recover;
            }

            return VideoFailureAction.None;
        }
    }
}
