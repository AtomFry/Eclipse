using Eclipse.Helpers;
using System;

namespace Eclipse.Service
{
    // Records what the video preview pipeline actually did, so a failure that only shows up
    // after hours of use can explain itself instead of having to be caught live.
    //
    // The failure being chased: previews stop playing after a long session and never recover
    // without restarting Big Box, while background artwork keeps working. That rules out the
    // animation chain, so playback is being requested and not happening. The counters below
    // say how many files were opened before it broke, and the checkpoints say how far each
    // attempt got - requested, opened, started - which separates "the file never opened" from
    // "it opened and refused to play".
    public sealed class VideoPlaybackMonitor
    {
        private readonly object countLock = new object();

        private int opensRequested;
        private int opensCompleted;
        private int playsRequested;
        private int playsStarted;
        private int failures;

        // A single failure proves nothing - a slow file or a selection that moved on can
        // produce one. A run of them with nothing playing in between is what says the media
        // stack is in trouble, so recovery is driven by this rather than by the total.
        private int consecutiveFailures;

        private string currentPath;

        public int ConsecutiveFailures
        {
            get
            {
                lock (countLock)
                {
                    return consecutiveFailures;
                }
            }
        }

        /// <summary>A media file is about to be handed to the player.</summary>
        public void OpenRequested(string videoPath)
        {
            lock (countLock)
            {
                opensRequested++;
                currentPath = videoPath;
            }
        }

        /// <summary>The player raised MediaOpened, so the file is genuinely loaded.</summary>
        public void OpenCompleted()
        {
            lock (countLock)
            {
                opensCompleted++;
            }
        }

        public void PlayRequested()
        {
            lock (countLock)
            {
                playsRequested++;
            }
        }

        /// <summary>Playback is running, so whatever went wrong before has cleared.</summary>
        public void PlayStarted()
        {
            lock (countLock)
            {
                playsStarted++;
                consecutiveFailures = 0;
            }
        }

        /// <summary>
        /// Playback was asked for and the position never advanced. This is the symptom that
        /// has been impossible to reproduce on demand, so it is logged with the full session
        /// counts - the gap between opens requested and opens completed is the tell for the
        /// media stack having run out of something.
        /// </summary>
        public void PlayDidNotStart()
        {
            LogFailure("Video preview did not start playing.");
        }

        public void OpenFailed(Exception exception)
        {
            LogFailure("Video preview failed to open.");
            LogHelper.LogException(exception, "open the selected game's video");
        }

        public void RecoveryAttempted()
        {
            LogHelper.Log($"Video preview recovery attempted - closing and reopening on the next selection. {Summary()}");
        }

        public void RecoveryAbandoned()
        {
            LogHelper.Log($"Video preview recovery failed repeatedly - no further attempts this session. {Summary()}");
        }

        public string Summary()
        {
            lock (countLock)
            {
                return $"Session totals - opens requested: {opensRequested}, opens completed: {opensCompleted}, "
                     + $"plays requested: {playsRequested}, plays started: {playsStarted}, failures: {failures} "
                     + $"({consecutiveFailures} in a row).";
            }
        }

        private void LogFailure(string what)
        {
            string path;
            lock (countLock)
            {
                failures++;
                consecutiveFailures++;
                path = currentPath;
            }

            LogHelper.Log($"{what} File: {path ?? "(none)"}. {Summary()}");
        }
    }
}
