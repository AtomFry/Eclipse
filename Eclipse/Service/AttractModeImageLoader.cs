using Eclipse.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Eclipse.Service
{
    // Decodes a screen saver image away from the UI thread.
    //
    // Attract mode used to call new BitmapImage(uri) on the UI thread once per slide, which
    // decodes the whole file synchronously - a visible hitch at the start of every slide, and
    // worst on the 4K artwork the pan exists to show off.
    //
    // A BitmapImage can be built on any thread as long as it is frozen before it is handed
    // over, and CacheOption.OnLoad is what forces the decode to happen here rather than
    // lazily on first render back on the UI thread.
    public static class AttractModeImageLoader
    {
        /// <summary>
        /// Decodes an image on a background thread and freezes it for use on the UI thread.
        /// Returns null for a null Uri, and for anything that fails to load - a missing or
        /// corrupt file should cost the slide its artwork, not stop the slideshow.
        /// </summary>
        public static async Task<ImageSource> LoadAsync(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            return await Task.Run<ImageSource>(() =>
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
                    LogHelper.LogException(ex, $"load the attract mode image at {uri}");
                    return null;
                }
            });
        }
    }
}
