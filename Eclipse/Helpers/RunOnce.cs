using System;
using System.Threading.Tasks;

namespace Eclipse.Helpers
{
    /// <summary>
    /// Runs one piece of work once, and lets every caller wait for it to finish.
    ///
    /// The obvious way to write "do this once" is a bool checked and set before the work
    /// begins. That is not once-only - it is at-most-once-*started*. A second caller arriving
    /// while the first is still working reads the flag, is told the work is done, and carries
    /// on against state that is still half built.
    ///
    /// That was a real defect, not a theoretical one: <see cref="Eclipse.Models.GameFiles"/>
    /// set its flag before resolving a game's media, so the screen saver could ask for a game
    /// the hydration pump had just started, be told it was ready, and find every path still
    /// null - which is why it showed the placeholder background for games that had one of
    /// their own.
    ///
    /// The fix is to make the handle the *task* rather than a flag, so "already running" and
    /// "already finished" stop being the same answer.
    /// </summary>
    public sealed class RunOnce
    {
        private readonly object gate = new object();
        private readonly Action work;

        private Task task;
        private bool hasCompleted;

        public RunOnce(Action work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            this.work = work;
        }

        /// <summary>
        /// True once the work has finished - successfully or not. Never true while it is still
        /// running, which is the whole point of this class.
        ///
        /// A caller that needs the work's results should await <see cref="RunAsync"/> rather
        /// than poll this. It exists for the callers that only need to know whether a piece of
        /// work is still outstanding, such as the media pump deciding what to hydrate next.
        /// </summary>
        public bool HasCompleted
        {
            get
            {
                lock (gate)
                {
                    return hasCompleted;
                }
            }
        }

        /// <summary>
        /// Starts the work if it has not started, and returns the task representing it. Every
        /// caller gets the same task, so every caller waits for the one run.
        /// </summary>
        public Task RunAsync()
        {
            lock (gate)
            {
                if (task == null)
                {
                    // Started under the lock but not *run* under it - the work itself can be
                    // slow, and holding the gate for its duration would serialise the callers
                    // this class exists to let through.
                    task = Task.Run(() => Execute());
                }

                return task;
            }
        }

        private void Execute()
        {
            try
            {
                work();
            }
            finally
            {
                // In a finally, so work that throws still counts as attempted. Retrying it
                // forever would be worse than living without whatever it was going to produce,
                // and the caller decides how loud the failure is - this class does not swallow
                // it.
                lock (gate)
                {
                    hasCompleted = true;
                }
            }
        }
    }
}
