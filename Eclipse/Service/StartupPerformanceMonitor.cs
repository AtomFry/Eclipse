using Eclipse.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace Eclipse.Service
{
    // Measures what startup actually does, and whether the UI thread is being kept from
    // rendering while it does it.
    //
    // The question this exists to answer is narrow: the lists freeze for a second or three when
    // the plugin first comes up, and the machine's fans spin. Both are consistent with several
    // different causes - the catalog build, the lazy image pre-scaling, the SAPI grammar, the
    // media pump, a burst of image decodes, or garbage collection triggered by any of them -
    // and they now all run at the same time, so guessing between them from the outside is not
    // possible.
    //
    // Three things are recorded:
    //
    //   Phases      - a named span of startup work, with when it started, how long it took, and
    //                 which thread ran it. Phases overlap on purpose: seeing that four of them
    //                 are in flight at once on a four-core laptop is itself the finding.
    //   UI stalls   - gaps between beats of a dispatcher heartbeat. This is the freeze the user
    //                 actually sees, timestamped so it can be lined up against the phases.
    //   Work        - counted and totalled per-game costs inside hydration, so the pump's share
    //                 can be split between filesystem scans and image processing.
    //
    // Off unless MeasureBrowsePerformance is set in EclipseSettings.json - the same switch the
    // browse monitor uses, so there is one thing to turn on. Every call site is guarded by
    // IsEnabled. Like BrowsePerformanceMonitor this is diagnostic scaffolding and comes out of
    // the tree once the startup work it is pointed at is settled.
    public sealed class StartupPerformanceMonitor
    {
        // A gap between heartbeats longer than this is reported. Two rendered frames at 60Hz is
        // 33ms and is normal; 150ms is where a person starts to perceive the UI as stuck.
        private const double StallThresholdMilliseconds = 150;

        // Short enough to place a stall's start within a frame or two, long enough that the
        // timer itself is not part of the problem.
        private const double HeartbeatMilliseconds = 50;

        // Enough to characterise a startup without turning the log into a transcript.
        private const int MaxStallsRecorded = 40;
        private const int MaxPhasesRecorded = 200;

        // Startup is over long before this. It exists so that a run whose pump never finishes
        // still produces a report rather than silently keeping the heartbeat alive.
        private const double AutoReportAfterMilliseconds = 90000;

        private readonly object countLock = new object();

        // T+0 for everything reported. Started on first use, which is the first phase the
        // loading state opens.
        private readonly Stopwatch sinceStart = Stopwatch.StartNew();

        private readonly List<PhaseRecord> phases = new List<PhaseRecord>();
        private readonly List<string> stalls = new List<string>();
        private readonly Dictionary<string, Stat> work = new Dictionary<string, Stat>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> counters = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, Gauge> gauges = new Dictionary<string, Gauge>(StringComparer.Ordinal);

        private DispatcherTimer heartbeat;
        private double lastBeat;
        private int stallCount;
        private double totalStalledMilliseconds;
        private double worstStallMilliseconds;
        private bool reported;

        public bool IsEnabled => BrowsePerformanceMonitor.Instance.IsEnabled;

        #region phases

        /// <summary>
        /// Opens a named span of startup work. Dispose it when the work is done - the usual
        /// shape is a using block around the call being measured. Phases may overlap and may be
        /// opened from any thread; the report shows them in the order they started.
        /// </summary>
        public IDisposable Phase(string name)
        {
            if (!IsEnabled)
            {
                return NullScope.Instance;
            }

            return new PhaseScope(this, name);
        }

        /// <summary>A moment worth placing on the timeline, with no duration of its own.</summary>
        public void Mark(string name)
        {
            if (!IsEnabled)
            {
                return;
            }

            RecordPhase(new PhaseRecord
            {
                Name = name,
                StartMilliseconds = sinceStart.Elapsed.TotalMilliseconds,
                DurationMilliseconds = 0,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                IsMark = true
            });
        }

        private void RecordPhase(PhaseRecord record)
        {
            lock (countLock)
            {
                if (phases.Count < MaxPhasesRecorded)
                {
                    phases.Add(record);
                }
            }
        }

        #endregion

        #region per-game work

        /// <summary>
        /// A timestamp to hand back to <see cref="Work"/>. Returns zero and costs nothing when
        /// measurement is off, which matters because these call sites run once per game.
        /// </summary>
        public long Ticks()
        {
            return IsEnabled ? Stopwatch.GetTimestamp() : 0L;
        }

        /// <summary>
        /// Records one occurrence of a repeated piece of work - resolving a game's bezel,
        /// scaling one box front, and so on. Totals are reported per name.
        /// </summary>
        public void Work(string name, long startTicks)
        {
            if (!IsEnabled || (startTicks == 0L))
            {
                return;
            }

            double milliseconds = (Stopwatch.GetTimestamp() - startTicks) * 1000d / Stopwatch.Frequency;

            lock (countLock)
            {
                Stat stat;
                if (!work.TryGetValue(name, out stat))
                {
                    stat = new Stat();
                    work[name] = stat;
                }

                stat.Add(milliseconds);
            }
        }

        /// <summary>
        /// Counts occurrences of something that has no duration worth timing - a retry, a
        /// branch taken. Reported as a plain tally.
        /// </summary>
        public void Count(string name)
        {
            if (!IsEnabled)
            {
                return;
            }

            lock (countLock)
            {
                long current;
                counters.TryGetValue(name, out current);
                counters[name] = current + 1;
            }
        }

        /// <summary>
        /// Tracks how many of something are happening at once, and the high-water mark. Dispose
        /// the returned scope when the work ends.
        ///
        /// This is the measurement that separates "the work is slow" from "the work is queued
        /// behind other copies of itself" - a high-water mark far above the processor count
        /// means the thread pool is being asked for more than it can run.
        /// </summary>
        public IDisposable Concurrency(string name)
        {
            if (!IsEnabled)
            {
                return NullScope.Instance;
            }

            lock (countLock)
            {
                Gauge gauge;
                if (!gauges.TryGetValue(name, out gauge))
                {
                    gauge = new Gauge();
                    gauges[name] = gauge;
                }

                gauge.Enter();
            }

            return new GaugeScope(this, name);
        }

        private void LeaveConcurrency(string name)
        {
            lock (countLock)
            {
                Gauge gauge;
                if (gauges.TryGetValue(name, out gauge))
                {
                    gauge.Leave();
                }
            }
        }

        #endregion

        #region UI stalls

        /// <summary>
        /// Starts a heartbeat on the UI dispatcher and records the gaps between its beats. A gap
        /// longer than the threshold means the UI thread was not able to run the timer, which is
        /// the freeze as the user experiences it rather than as a piece of work claims it.
        ///
        /// Deliberately queued at Background priority, below input, layout and render: a beat
        /// delayed because the dispatcher is busy laying out a row is as much a stall to the
        /// person watching as one delayed by a blocked thread.
        /// </summary>
        public void StartUiStallWatch(Dispatcher dispatcher)
        {
            if (!IsEnabled || (dispatcher == null))
            {
                return;
            }

            lock (countLock)
            {
                if (heartbeat != null)
                {
                    return;
                }

                lastBeat = sinceStart.Elapsed.TotalMilliseconds;

                heartbeat = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(HeartbeatMilliseconds)
                };
            }

            heartbeat.Tick += OnHeartbeat;
            heartbeat.Start();

            Mark("UI stall watch started");
        }

        private void OnHeartbeat(object sender, EventArgs e)
        {
            double now = sinceStart.Elapsed.TotalMilliseconds;
            bool shouldAutoReport;

            lock (countLock)
            {
                double gap = now - lastBeat;
                lastBeat = now;

                if (gap >= StallThresholdMilliseconds)
                {
                    stallCount++;
                    totalStalledMilliseconds += gap - HeartbeatMilliseconds;

                    if (gap > worstStallMilliseconds)
                    {
                        worstStallMilliseconds = gap;
                    }

                    if (stalls.Count < MaxStallsRecorded)
                    {
                        // The counters are sampled here rather than at the start of the gap
                        // because there is nowhere to sample them from during it - the UI thread
                        // was not running. What they show is the state the stall left behind.
                        stalls.Add($"    {Round(gap)}ms stall ending at T+{Round(now)}ms "
                                 + $"[{GcSnapshot()}, {PoolSnapshot()}]");
                    }
                }

                shouldAutoReport = !reported && (now > AutoReportAfterMilliseconds);
            }

            if (shouldAutoReport)
            {
                Report("timed out waiting for the media pump");
            }
        }

        private void StopUiStallWatch()
        {
            DispatcherTimer timer;

            lock (countLock)
            {
                timer = heartbeat;
                heartbeat = null;
            }

            if (timer != null)
            {
                // Started on the dispatcher, so stopped on it too. Posted rather than
                // invoked: this is normally reached from the media pump thread, and a diagnostic
                // has no business blocking on the UI thread it is measuring.
                timer.Dispatcher.BeginInvoke(new Action(() =>
                {
                    timer.Stop();
                    timer.Tick -= OnHeartbeat;
                }));
            }
        }

        #endregion

        #region reporting

        /// <summary>
        /// Writes everything gathered so far as a single log entry and stops the heartbeat.
        /// Called once, when the media pump finishes - that is the last piece of startup work to
        /// end. Later calls are ignored.
        /// </summary>
        public void Report(string reason)
        {
            if (!IsEnabled)
            {
                return;
            }

            lock (countLock)
            {
                if (reported)
                {
                    return;
                }

                reported = true;
            }

            string summary = BuildReport(reason);

            StopUiStallWatch();

            // One entry rather than one per line: LogHelper opens and closes the file per call.
            LogHelper.Log(summary);
        }

        private string BuildReport(string reason)
        {
            StringBuilder report = new StringBuilder();

            lock (countLock)
            {
                report.AppendLine($"Startup performance ({reason}). Machine: {Environment.ProcessorCount} logical processors.");
                report.AppendLine($"  Totals at T+{Round(sinceStart.Elapsed.TotalMilliseconds)}ms: {GcSnapshot()}, {PoolSnapshot()}.");

                report.AppendLine("  Phases, in the order they started. Overlapping spans ran concurrently:");
                if (phases.Count == 0)
                {
                    report.AppendLine("    none recorded.");
                }
                else
                {
                    phases.Sort((left, right) => left.StartMilliseconds.CompareTo(right.StartMilliseconds));

                    foreach (PhaseRecord phase in phases)
                    {
                        if (phase.IsMark)
                        {
                            report.AppendLine($"    T+{Round(phase.StartMilliseconds)}ms  ---  {phase.Name} (thread {phase.ThreadId})");
                        }
                        else
                        {
                            report.AppendLine($"    T+{Round(phase.StartMilliseconds)}ms  {Round(phase.DurationMilliseconds)}ms  {phase.Name} "
                                            + $"(thread {phase.ThreadId}, gen0 +{phase.Gen0}, gen1 +{phase.Gen1}, gen2 +{phase.Gen2}, "
                                            + $"allocated {Megabytes(phase.AllocatedBytes)}MB, GC paused {Round(phase.GcPauseMilliseconds)}ms)");
                        }
                    }
                }

                report.AppendLine($"  UI stalls over {Round(StallThresholdMilliseconds)}ms: {stallCount}, "
                                + $"worst {Round(worstStallMilliseconds)}ms, {Round(totalStalledMilliseconds)}ms unresponsive in total.");
                if (stalls.Count == 0)
                {
                    report.AppendLine("    none recorded.");
                }
                else
                {
                    foreach (string stall in stalls)
                    {
                        report.AppendLine(stall);
                    }

                    if (stallCount > stalls.Count)
                    {
                        report.AppendLine($"    ... and {stallCount - stalls.Count} more not listed.");
                    }
                }

                report.AppendLine("  How many ran at once (high-water mark against " + Environment.ProcessorCount + " processors):");
                if (gauges.Count == 0)
                {
                    report.AppendLine("    none recorded.");
                }
                else
                {
                    foreach (KeyValuePair<string, Gauge> gauge in gauges)
                    {
                        report.AppendLine($"    {gauge.Key}: peak {gauge.Value.Max} at once, {gauge.Value.Total} in total, {gauge.Value.Current} still running");
                    }
                }

                report.AppendLine("  Counters:");
                if (counters.Count == 0)
                {
                    report.AppendLine("    none recorded.");
                }
                else
                {
                    foreach (KeyValuePair<string, long> counter in counters)
                    {
                        report.AppendLine($"    {counter.Key}: {counter.Value}");
                    }
                }

                report.AppendLine("  Repeated work, totalled:");
                if (work.Count == 0)
                {
                    report.AppendLine("    none recorded.");
                }
                else
                {
                    List<string> names = new List<string>(work.Keys);
                    names.Sort((left, right) => work[right].Total.CompareTo(work[left].Total));

                    foreach (string name in names)
                    {
                        Stat stat = work[name];
                        report.AppendLine($"    {name}: {stat.Count} calls, {Round(stat.Total)}ms total, "
                                        + $"avg {Round(stat.Average)}ms, max {Round(stat.Max)}ms");
                    }
                }
            }

            return report.ToString();
        }

        private static string GcSnapshot()
        {
            return $"gen0 {GC.CollectionCount(0)}, gen1 {GC.CollectionCount(1)}, gen2 {GC.CollectionCount(2)}, "
                 + $"heap {Megabytes(GC.GetTotalMemory(false))}MB, GC paused {Round(GC.GetTotalPauseDuration().TotalMilliseconds)}ms";
        }

        private static string PoolSnapshot()
        {
            return $"thread pool {ThreadPool.ThreadCount} threads, {ThreadPool.PendingWorkItemCount} queued";
        }

        private static double Round(double value)
        {
            return Math.Round(value, 1);
        }

        private static string Megabytes(long bytes)
        {
            return Math.Round(bytes / 1024d / 1024d, 1).ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        private struct PhaseRecord
        {
            public string Name;
            public double StartMilliseconds;
            public double DurationMilliseconds;
            public int ThreadId;
            public bool IsMark;
            public int Gen0;
            public int Gen1;
            public int Gen2;
            public long AllocatedBytes;
            public double GcPauseMilliseconds;
        }

        // A phase's span, closed by disposing it. Captures the collection counts and the
        // allocation and GC pause totals across the span, so a phase that is slow because it is
        // allocating heavily can be told apart from one that is slow because it is waiting.
        private sealed class PhaseScope : IDisposable
        {
            private readonly StartupPerformanceMonitor monitor;
            private readonly string name;
            private readonly double startMilliseconds;
            private readonly Stopwatch stopwatch;
            private readonly int gen0;
            private readonly int gen1;
            private readonly int gen2;
            private readonly long allocatedBytes;
            private readonly TimeSpan gcPause;
            private bool disposed;

            public PhaseScope(StartupPerformanceMonitor monitor, string name)
            {
                this.monitor = monitor;
                this.name = name;

                startMilliseconds = monitor.sinceStart.Elapsed.TotalMilliseconds;
                gen0 = GC.CollectionCount(0);
                gen1 = GC.CollectionCount(1);
                gen2 = GC.CollectionCount(2);
                allocatedBytes = GC.GetTotalAllocatedBytes(false);
                gcPause = GC.GetTotalPauseDuration();
                stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                stopwatch.Stop();

                monitor.RecordPhase(new PhaseRecord
                {
                    Name = name,
                    StartMilliseconds = startMilliseconds,
                    DurationMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                    Gen0 = GC.CollectionCount(0) - gen0,
                    Gen1 = GC.CollectionCount(1) - gen1,
                    Gen2 = GC.CollectionCount(2) - gen2,
                    AllocatedBytes = GC.GetTotalAllocatedBytes(false) - allocatedBytes,
                    GcPauseMilliseconds = (GC.GetTotalPauseDuration() - gcPause).TotalMilliseconds
                });
            }
        }

        // What Phase returns when measurement is off, so call sites stay a plain using block.
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            private NullScope()
            {
            }

            public void Dispose()
            {
            }
        }

        // How many of something are in flight, and the most there have ever been at once.
        private sealed class Gauge
        {
            public long Current { get; private set; }
            public long Max { get; private set; }
            public long Total { get; private set; }

            public void Enter()
            {
                Current++;
                Total++;
                if (Current > Max) { Max = Current; }
            }

            public void Leave()
            {
                Current--;
            }
        }

        private sealed class GaugeScope : IDisposable
        {
            private readonly StartupPerformanceMonitor monitor;
            private readonly string name;
            private bool disposed;

            public GaugeScope(StartupPerformanceMonitor monitor, string name)
            {
                this.monitor = monitor;
                this.name = name;
            }

            public void Dispose()
            {
                if (disposed) { return; }
                disposed = true;
                monitor.LeaveConcurrency(name);
            }
        }

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
        }

        #region singleton implementation

        public static StartupPerformanceMonitor Instance => instance;

        private static readonly StartupPerformanceMonitor instance = new StartupPerformanceMonitor();

        static StartupPerformanceMonitor()
        {
        }

        private StartupPerformanceMonitor()
        {
        }

        #endregion
    }
}
