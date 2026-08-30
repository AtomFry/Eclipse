using Eclipse.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// The claim that matters here is not that the work runs once - a bool does that. It is
    /// that a caller arriving while the work is still running <em>waits for it</em>, instead of
    /// being told it is finished and reading half-built state.
    ///
    /// That distinction is the defect this replaced: <c>GameFiles.SetupFiles</c> set its
    /// "already done" flag before resolving anything, so the screen saver could ask the pump for
    /// a game it had just started, be told it was ready, and find every path still null.
    /// <see cref="Second_caller_arriving_mid_run_waits_for_the_work_to_finish"/> is the test
    /// that fails against the old design and passes against this one.
    /// </summary>
    public class RunOnceTests
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task Second_caller_arriving_mid_run_waits_for_the_work_to_finish()
        {
            ManualResetEventSlim workStarted = new ManualResetEventSlim();
            ManualResetEventSlim letWorkFinish = new ManualResetEventSlim();
            bool populated = false;

            RunOnce once = new RunOnce(() =>
            {
                workStarted.Set();
                letWorkFinish.Wait(Patience);
                populated = true;
            });

            Task first = once.RunAsync();

            // wait until the work is genuinely in flight, so the second caller cannot arrive
            // before it starts and pass for the wrong reason
            Assert.True(workStarted.Wait(Patience), "the work never started");

            Task second = once.RunAsync();

            // the flag-before-work version reported true here, which is what made the caller
            // carry on against nothing
            Assert.False(once.HasCompleted);

            letWorkFinish.Set();
            await second;

            Assert.True(populated);

            await first;
        }

        [Fact]
        public async Task Work_runs_once_however_many_callers_arrive_together()
        {
            const int callers = 32;

            ManualResetEventSlim letWorkFinish = new ManualResetEventSlim();
            int executions = 0;

            RunOnce once = new RunOnce(() =>
            {
                Interlocked.Increment(ref executions);
                letWorkFinish.Wait(Patience);
            });

            // started from separate threads rather than in a loop, so they genuinely contend
            Task[] all = new Task[callers];
            using (Barrier startTogether = new Barrier(callers))
            {
                for (int caller = 0; caller < callers; caller++)
                {
                    all[caller] = Task.Run(() =>
                    {
                        startTogether.SignalAndWait(Patience);
                        return once.RunAsync();
                    });
                }

                letWorkFinish.Set();
                await Task.WhenAll(all);
            }

            Assert.Equal(1, Volatile.Read(ref executions));
            Assert.True(once.HasCompleted);
        }

        [Fact]
        public async Task Callers_after_completion_do_not_run_the_work_again()
        {
            int executions = 0;
            RunOnce once = new RunOnce(() => Interlocked.Increment(ref executions));

            await once.RunAsync();
            await once.RunAsync();
            await once.RunAsync();

            Assert.Equal(1, Volatile.Read(ref executions));
            Assert.True(once.HasCompleted);
        }

        [Fact]
        public void Nothing_has_completed_before_it_is_asked_to_run()
        {
            int executions = 0;
            RunOnce once = new RunOnce(() => Interlocked.Increment(ref executions));

            Assert.False(once.HasCompleted);
            Assert.Equal(0, Volatile.Read(ref executions));
        }

        /// <summary>
        /// Work that throws still counts as attempted, and the failure reaches the caller rather
        /// than being swallowed here. <c>GameFiles</c> catches and logs its own failures so the
        /// media pump is never handed a faulted task; that is its decision to make, not this
        /// class's.
        /// </summary>
        [Fact]
        public async Task Work_that_throws_is_attempted_once_and_the_failure_reaches_the_caller()
        {
            int executions = 0;
            RunOnce once = new RunOnce(() =>
            {
                Interlocked.Increment(ref executions);
                throw new InvalidOperationException("media resolution failed");
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => once.RunAsync());

            Assert.True(once.HasCompleted);

            await Assert.ThrowsAsync<InvalidOperationException>(() => once.RunAsync());
            Assert.Equal(1, Volatile.Read(ref executions));
        }

        [Fact]
        public void Work_is_required()
        {
            Assert.Throws<ArgumentNullException>(() => new RunOnce(null));
        }
    }
}
