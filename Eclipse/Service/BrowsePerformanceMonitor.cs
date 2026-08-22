using Eclipse.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Eclipse.Service
{
    // Measures what a keypress in the box art row actually costs, and what the background media
    // pump does while that is happening.
    //
    // The row refactor cannot be judged without this. TRACEABILITY.md already says B-31 is
    // "possibly premature ... needs B-28 latency data before it is treated as debt at all", and
    // the same applies to every stage that follows: nobody can currently say whether a keypress
    // costs 2ms or 40ms, or where the time goes.
    //
    // Off unless MeasureBrowsePerformance is set in EclipseSettings.json. Every call site is
    // guarded by IsEnabled so that a normal session pays nothing - which matters here more than
    // usual, because one of the counters ticks tens of millions of times.
    //
    // Comes out of the tree when the row refactor is finished, like the B-15 dump machinery.
    public sealed class BrowsePerformanceMonitor
    {
        // A hold-right run produces output without needing a session-end hook, and 25 is low
        // enough that a deliberate 30-press walk also reports.
        private const int MovesPerSummary = 25;

        private readonly object countLock = new object();

        // Per-move timings. A "move" is one navigation keypress: the cycle advances, the slots
        // are reassigned, and eventually the UI catches up.
        private int moves;

        // Every measurement is kept twice: once for the session, and once for the last summary's
        // worth of moves. A session average hides a problem that only appears later - which is
        // exactly the shape of the one being looked for, since the weak image cache is evicted
        // under memory pressure that builds over a session. Reading the window figures down
        // successive log lines shows the trend; a cumulative average would dilute it away.
        private readonly Stat sessionToSlots = new Stat();
        private readonly Stat windowToSlots = new Stat();
        private readonly Stat sessionToRender = new Stat();
        private readonly Stat windowToRender = new Stat();
        private readonly Stat sessionAnimate = new Stat();
        private readonly Stat windowAnimate = new Stat();

        // How many of the row's slots actually took a new game. This is the number the refactor
        // is aiming at: it should be the window size today and 1 afterwards.
        private readonly Stat sessionSlotsChanged = new Stat();
        private readonly Stat windowSlotsChanged = new Stat();

        // Distinct vs repeat artwork passing through the row. See the note on Summary() for why
        // this is here instead of a decode count.
        private readonly HashSet<string> distinctRowImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private long rowImagesShown;

        // What a row image actually costs once decoded, measured rather than estimated. A
        // decoded bitmap is width x height x bytes-per-pixel and owes nothing to the size of the
        // file it came from, so a 40KB png can be a third of a megabyte in memory. This is the
        // number the frozen-image cache is sized from: hold one per game across a large library
        // and it reaches gigabytes, so how many to keep has to be a measurement rather than a
        // guess.
        private const int DecodedSizeSamples = 25;

        private int decodedSamplesTaken;
        private int decodedSamplesRequested;
        private long decodedBytesTotal;
        private long decodedBytesMin = long.MaxValue;
        private long decodedBytesMax;
        private long sourceFileBytesTotal;
        private int sampleWidth;
        private int sampleHeight;
        private int sampleBitsPerPixel;

        private int librarySize;

        // The media hydration pump.
        private long pumpIterations;
        private long pumpPredicateEvaluations;
        private readonly Stopwatch pumpDuration = new Stopwatch();

        // In-flight state for the current move.
        private Stopwatch moveTimer;
        private bool moveAwaitingRender;

        public bool IsEnabled { get; }

        private BrowsePerformanceMonitor()
        {
            bool enabled = false;

            try
            {
                enabled = EclipseSettingsDataProvider.Instance.EclipseSettings.MeasureBrowsePerformance;
            }
            catch (Exception ex)
            {
                // Settings are read from disk and this runs from a static initializer, so a
                // failure here must not take the plugin down with it.
                LogHelper.LogException(ex, "read the browse performance setting");
            }

            IsEnabled = enabled;
        }

        #region moves

        /// <summary>A navigation keypress has arrived and the cycle is about to advance.</summary>
        public void MoveStarted()
        {
            if (!IsEnabled) { return; }

            lock (countLock)
            {
                moveTimer = Stopwatch.StartNew();
                moveAwaitingRender = true;
            }
        }

        /// <summary>
        /// The row's slots have been reassigned. Called for every refresh, including the ones no
        /// keypress caused - a list rebuild, or a jump to a random game - so the timing is only
        /// recorded when a move is actually in flight.
        /// </summary>
        public void SlotsAssigned(int changed, int slotCount)
        {
            if (!IsEnabled) { return; }

            lock (countLock)
            {
                sessionSlotsChanged.Add(changed);
                windowSlotsChanged.Add(changed);

                if (moveTimer != null)
                {
                    double elapsed = moveTimer.Elapsed.TotalMilliseconds;
                    moves++;
                    sessionToSlots.Add(elapsed);
                    windowToSlots.Add(elapsed);
                }
            }
        }

        public void AnimateGameChangeCompleted(double milliseconds)
        {
            if (!IsEnabled) { return; }

            lock (countLock)
            {
                sessionAnimate.Add(milliseconds);
                windowAnimate.Add(milliseconds);
            }
        }

        /// <summary>
        /// The dispatcher has finished the render pass that the slot assignment caused, so the
        /// user can see the new row. This is where a synchronous image decode lands.
        /// </summary>
        public void RenderCompleted()
        {
            if (!IsEnabled) { return; }

            bool shouldLog = false;
            string summary = null;

            lock (countLock)
            {
                if ((moveTimer != null) && moveAwaitingRender)
                {
                    double elapsed = moveTimer.Elapsed.TotalMilliseconds;
                    sessionToRender.Add(elapsed);
                    windowToRender.Add(elapsed);

                    moveAwaitingRender = false;
                    moveTimer = null;

                    shouldLog = (moves > 0) && (moves % MovesPerSummary == 0);
                }

                if (shouldLog)
                {
                    summary = MoveSummary();
                    ResetWindow();
                }
            }

            if (summary != null)
            {
                LogHelper.Log($"Browse performance - {summary}");
            }
        }

        // The window figures cover the moves since the last summary, so they start again once it
        // has been taken. The session figures never reset.
        private void ResetWindow()
        {
            windowToSlots.Reset();
            windowToRender.Reset();
            windowAnimate.Reset();
            windowSlotsChanged.Reset();
        }

        /// <summary>Artwork that has just been put into a row slot.</summary>
        public void RowImageShown(Uri image)
        {
            if (!IsEnabled || (image == null)) { return; }

            bool isNew;
            bool shouldSample;

            lock (countLock)
            {
                rowImagesShown++;
                isNew = distinctRowImages.Add(image.OriginalString);

                shouldSample = isNew && (decodedSamplesRequested < DecodedSizeSamples);
                if (shouldSample) { decodedSamplesRequested++; }
            }

            if (shouldSample)
            {
                MeasureDecodedSize(image);
            }
        }

        /// <summary>
        /// Decodes one row image away from the UI thread purely to find out how much memory it
        /// occupies once decoded. Deliberately capped at a handful of samples: this is a
        /// measurement, and decoding the whole library to measure it would be its own problem.
        /// The result is discarded immediately - only its dimensions are kept.
        /// </summary>
        private void MeasureDecodedSize(Uri image)
        {
            Task.Run(() =>
            {
                try
                {
                    BitmapImage decoded = new BitmapImage();
                    decoded.BeginInit();
                    decoded.UriSource = image;
                    decoded.CacheOption = BitmapCacheOption.OnLoad;
                    decoded.EndInit();
                    decoded.Freeze();

                    int bitsPerPixel = decoded.Format.BitsPerPixel;
                    if (bitsPerPixel <= 0) { bitsPerPixel = 32; }

                    long decodedBytes = (long)decoded.PixelWidth * decoded.PixelHeight * bitsPerPixel / 8;

                    long fileBytes = 0;
                    if (image.IsFile)
                    {
                        FileInfo fileInfo = new FileInfo(image.LocalPath);
                        if (fileInfo.Exists) { fileBytes = fileInfo.Length; }
                    }

                    string completedSummary = null;

                    lock (countLock)
                    {
                        decodedSamplesTaken++;
                        decodedBytesTotal += decodedBytes;
                        sourceFileBytesTotal += fileBytes;

                        if (decodedBytes < decodedBytesMin) { decodedBytesMin = decodedBytes; }
                        if (decodedBytes > decodedBytesMax)
                        {
                            decodedBytesMax = decodedBytes;
                            sampleWidth = decoded.PixelWidth;
                            sampleHeight = decoded.PixelHeight;
                            sampleBitsPerPixel = bitsPerPixel;
                        }

                        // Report once, as soon as the sample is complete, rather than only as
                        // part of a move summary. This number decides the frozen-image cache's
                        // size and should not depend on the user having scrolled far enough.
                        if (decodedSamplesTaken == DecodedSizeSamples)
                        {
                            completedSummary = DecodedSizeSummary();
                        }
                    }

                    if (completedSummary != null)
                    {
                        LogHelper.Log($"Browse performance - {completedSummary}");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, $"measure the decoded size of {image}");
                }
            });
        }

        #endregion

        #region hydration pump

        public void PumpStarted(int gamesInLibrary)
        {
            if (!IsEnabled) { return; }

            lock (countLock)
            {
                librarySize = gamesInLibrary;
                pumpDuration.Restart();
            }
        }

        public void PumpIterationCompleted()
        {
            if (!IsEnabled) { return; }

            lock (countLock)
            {
                pumpIterations++;
            }
        }

        /// <summary>
        /// One evaluation of an "is this game already set up" predicate. This is the hot one -
        /// the current design evaluates it on the order of n squared times over a full pass,
        /// which is the whole reason it is being counted.
        /// </summary>
        public void PumpPredicateEvaluated()
        {
            if (!IsEnabled) { return; }

            lock (countLock)
            {
                pumpPredicateEvaluations++;
            }
        }

        public void PumpFinished(int gamesSetUp, int gamesTotal)
        {
            if (!IsEnabled) { return; }

            lock (countLock)
            {
                pumpDuration.Stop();
            }

            LogHelper.Log($"Browse performance - media pump finished. {PumpSummary(gamesSetUp, gamesTotal)}");
        }

        #endregion

        public string MoveSummary()
        {
            lock (countLock)
            {
                if (moves == 0)
                {
                    return "no moves recorded.";
                }

                return $"moves: {moves} ({windowToSlots.Count} in this window). "
                     + $"Keypress to slots assigned: {Compare(windowToSlots, sessionToSlots)}. "
                     + $"Keypress to rendered: {Compare(windowToRender, sessionToRender)}. "
                     + $"Animate game change: {Compare(windowAnimate, sessionAnimate)}. "
                     + $"Slots changed per assignment: window avg {Round(windowSlotsChanged.Average)} max {Round(windowSlotsChanged.Max)} "
                     + $"| session avg {Round(sessionSlotsChanged.Average)} max {Round(sessionSlotsChanged.Max)} "
                     + $"over {sessionSlotsChanged.Count} assignments. "
                     + $"Row artwork: {rowImagesShown} shown, {distinctRowImages.Count} distinct. "
                     + DecodedSizeSummary();
            }
        }

        // Window first, session second. The pair is the point: if the window figures climb down
        // successive log lines while the session average stays flat, something is degrading as
        // the session goes on - which is the signature of the weak image cache being evicted.
        private static string Compare(Stat window, Stat session)
        {
            return $"window avg {Round(window.Average)}ms max {Round(window.Max)}ms "
                 + $"| session avg {Round(session.Average)}ms max {Round(session.Max)}ms";
        }

        /// <summary>
        /// What a row image costs in memory once decoded, and what holding one per game would
        /// cost across this library. Caller must hold countLock.
        /// </summary>
        private string DecodedSizeSummary()
        {
            if (decodedSamplesTaken == 0)
            {
                return "Decoded size: no samples yet.";
            }

            long decodedAverage = decodedBytesTotal / decodedSamplesTaken;
            long fileAverage = sourceFileBytesTotal / decodedSamplesTaken;

            string summary = $"Decoded row image over {decodedSamplesTaken} samples: "
                           + $"avg {Kilobytes(decodedAverage)}KB, min {Kilobytes(decodedBytesMin)}KB, max {Kilobytes(decodedBytesMax)}KB "
                           + $"(largest was {sampleWidth}x{sampleHeight} at {sampleBitsPerPixel}bpp). "
                           + $"Source files avg {Kilobytes(fileAverage)}KB on disk.";

            // The number the retention policy is chosen from. Holding one decoded bitmap per
            // game is the simplest possible cache and the reason it is not the chosen one.
            if (librarySize > 0)
            {
                summary += $" Holding one per game across {librarySize} games would be "
                         + $"{Megabytes(decodedAverage * librarySize)}MB; "
                         + $"holding 100 would be {Megabytes(decodedAverage * 100)}MB.";
            }

            return summary;
        }

        private static double Kilobytes(long bytes)
        {
            return Math.Round(bytes / 1024d, 1);
        }

        private static double Megabytes(long bytes)
        {
            return Math.Round(bytes / 1024d / 1024d, 1);
        }

        public string PumpSummary(int gamesSetUp, int gamesTotal)
        {
            lock (countLock)
            {
                return $"Iterations: {pumpIterations}. "
                     + $"Predicate evaluations: {pumpPredicateEvaluations}. "
                     + $"Elapsed: {Round(pumpDuration.Elapsed.TotalMilliseconds)}ms. "
                     + $"Games set up: {gamesSetUp} of {gamesTotal}.";
            }
        }

        private static double Round(double value)
        {
            return Math.Round(value, 2);
        }

        // Count, average and max for one measurement. Kept as a type rather than three fields
        // each so that every measurement can be tracked twice - session and window - without
        // eight more fields per figure.
        private sealed class Stat
        {
            public long Count { get; private set; }
            public double Total { get; private set; }
            public double Max { get; private set; }

            public double Average => Count == 0 ? 0 : Total / Count;

            public void Add(double value)
            {
                Count++;
                Total += value;
                if (value > Max) { Max = value; }
            }

            public void Reset()
            {
                Count = 0;
                Total = 0;
                Max = 0;
            }
        }

        #region singleton implementation

        public static BrowsePerformanceMonitor Instance => instance;

        private static readonly BrowsePerformanceMonitor instance = new BrowsePerformanceMonitor();

        static BrowsePerformanceMonitor()
        {
        }

        #endregion
    }
}
