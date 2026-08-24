using Eclipse.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Eclipse.Service
{
    // Decodes an image away from the UI thread and freezes it for use on the UI thread.
    //
    // Both the screen saver and the browsing path used to call new BitmapImage(uri) directly on
    // the UI thread, which decodes the whole file synchronously - a visible hitch per slide, and
    // five decodes per keypress while scrolling through games.
    //
    // A BitmapImage can be built on any thread as long as it is frozen before it is handed
    // over, and CacheOption.OnLoad is what forces the decode to happen here rather than
    // lazily on first render back on the UI thread.
    public static class FrozenImageLoader
    {
        // How many decodes may run at once, across the whole plugin.
        //
        // This is small on purpose, and the number came out of a measurement rather than a
        // preference. The row decoder warms a whole list window at once, so up to 29 decodes
        // were being started together; at that concurrency each one took an average of 1.5
        // seconds and the worst took 9.6, against the 3.5ms a single decode costs. The time was
        // not spent waiting for a thread pool thread - that averaged 259ms - it was spent inside
        // the decode itself. Decoding a BitmapImage off the UI thread does not scale the way an
        // ordinary CPU-bound loop would, so the answer is to stop asking it to.
        //
        // Two leaves a little overlap for the file read without reaching the concurrency where
        // the decodes start obstructing each other. A full window at 3.5ms each is about 100ms
        // even fully serialised, which is well inside the time a list change takes.
        private const int MaxConcurrentDecodes = 2;

        private static readonly SemaphoreSlim decodeGate = new SemaphoreSlim(MaxConcurrentDecodes);

        /// <summary>
        /// Decodes on a background thread and freezes, so the result is safe to hand to the UI.
        /// Returns null for a null Uri, and for anything that fails to load - a missing or
        /// corrupt file should cost the slide its artwork, not stop the slideshow.
        /// </summary>
        public static async Task<ImageSource> LoadAsync(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            // Split deliberately: the time from here to the moment the work item actually starts
            // is time spent waiting for a thread pool thread, and is a completely different
            // problem from the decode being slow once it gets one.
            StartupPerformanceMonitor startupMonitor = StartupPerformanceMonitor.Instance;
            long queuedTicks = startupMonitor.Ticks();

            // Awaited rather than held across a thread pool thread: a caller queued behind the
            // gate has no business occupying a worker while it waits. ConfigureAwait(false)
            // throughout keeps every continuation off the UI thread - two thirds of the decodes
            // measured were started from it, and each one that resumes there competes with
            // rendering for the dispatcher.
            await decodeGate.WaitAsync().ConfigureAwait(false);

            try
            {
                return await Task.Run<ImageSource>(() =>
                {
                    startupMonitor.Work("image: waited for a thread pool thread", queuedTicks);

                    long decodeTicks = startupMonitor.Ticks();

                    using (startupMonitor.Concurrency("image decodes"))
                    {
                        try
                        {
                            BitmapImage image = new BitmapImage();

                            image.BeginInit();
                            image.UriSource = uri;

                            // decode now, on this thread, and stop holding the file open
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.EndInit();

                            // required before another thread may touch it
                            image.Freeze();

                            return image;
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex, $"load the image at {uri}");
                            return null;
                        }
                        finally
                        {
                            startupMonitor.Work("image: the decode itself", decodeTicks);
                        }
                    }
                }).ConfigureAwait(false);
            }
            finally
            {
                decodeGate.Release();
            }
        }
    }
}
