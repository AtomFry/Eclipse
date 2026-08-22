using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Eclipse.Service
{
    // Decodes box art before it reaches the row, so the decode does not happen on the UI thread
    // during a keypress.
    //
    // Measured cause: binding a Uri to Image.Source makes WPF decode the file through
    // ImageSourceConverter, and PropertyChanged is raised synchronously inside the slot
    // assignment - so the decode lands inside RefreshGames. A move that brings artwork the user
    // has already seen costs about 4ms; a move that brings a game whose art has never been
    // decoded costs about 7.4ms, and the ~3.5ms difference is one 722KB image being decoded.
    //
    // Note what this is NOT. The long-session measurement showed no degradation over twenty
    // minutes, so WPF's weak image cache is not being evicted here and there is nothing to fix
    // by holding bitmaps longer. The cost is first view, and the answer to first view is to do
    // it earlier and elsewhere rather than to keep more of it.
    //
    // Memory is why this is bounded rather than a decode-once-per-game field. A decoded row
    // image measured 722KB on this display; one per game across a 694-game library would be
    // 489MB, and libraries get much larger than that.
    public sealed class RowImageDecoder
    {
        // The row shows 13 games and the next list shows 13 more. The rest is scroll headroom
        // in both directions - enough that turning round mid-scroll finds warm artwork.
        private const int MaxDecodedImages = 96;

        // How far past each end of the window to decode. A move takes roughly 40ms when a
        // direction is held and a decode takes roughly 3.5ms, so a handful of games of runway
        // is far more than the decoder needs to stay ahead.
        private const int LookAhead = 8;

        private readonly object cacheLock = new object();

        // Most-recently-used first. A list rather than a dictionary: it holds at most
        // MaxDecodedImages entries, so a scan is cheaper than the hashing would be, and the
        // order is the eviction policy.
        private readonly LinkedList<GameFiles> decoded = new LinkedList<GameFiles>();

        /// <summary>
        /// Decodes the artwork for the games in and around the window, if it is not decoded
        /// already. Returns immediately - the decoding happens on the thread pool and each image
        /// appears when it is ready.
        /// </summary>
        public void WarmWindow(IReadOnlyList<GameMatch> games, int windowStart, int windowLength)
        {
            if ((games == null) || (games.Count == 0))
            {
                return;
            }

            int first = windowStart - LookAhead;
            int last = windowStart + windowLength + LookAhead;

            for (int index = first; index <= last; index++)
            {
                // The window wraps, so the margin either side of it wraps too.
                int wrapped = ((index % games.Count) + games.Count) % games.Count;
                Warm(games[wrapped]?.GameFiles);
            }
        }

        private void Warm(GameFiles gameFiles)
        {
            if (gameFiles == null)
            {
                return;
            }

            lock (cacheLock)
            {
                LinkedListNode<GameFiles> existing = decoded.Find(gameFiles);
                if (existing != null)
                {
                    // Already decoded - move it to the front so that the games around the
                    // window are the last ones to be evicted.
                    decoded.Remove(existing);
                    decoded.AddFirst(existing);
                    return;
                }

                if (gameFiles.FrontImageDecodePending)
                {
                    return;
                }

                gameFiles.FrontImageDecodePending = true;
            }

            DecodeAsync(gameFiles);
        }

        private async void DecodeAsync(GameFiles gameFiles)
        {
            try
            {
                Uri source = gameFiles.FrontImage;
                ImageSource image = await FrozenImageLoader.LoadAsync(source);

                // Flip box swaps FrontImage while this is in flight. Decoding the old file and
                // assigning it would show the wrong side of the box, so drop the result and let
                // the swap request its own decode.
                if (!ReferenceEquals(source, gameFiles.FrontImage))
                {
                    return;
                }

                gameFiles.FrontImageSource = image;

                GameFiles evicted = null;

                lock (cacheLock)
                {
                    decoded.AddFirst(gameFiles);

                    if (decoded.Count > MaxDecodedImages)
                    {
                        evicted = decoded.Last.Value;
                        decoded.RemoveLast();
                    }
                }

                // Released outside the lock: this raises PropertyChanged, which the binding
                // engine marshals to the dispatcher, and holding a lock across that is asking
                // for trouble.
                if (evicted != null)
                {
                    evicted.FrontImageSource = null;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "decode row artwork ahead of the window");
            }
            finally
            {
                gameFiles.FrontImageDecodePending = false;
            }
        }

        /// <summary>
        /// The artwork path for a game has changed, so anything decoded from the old one is
        /// wrong. Called from the FrontImage setter.
        ///
        /// Two things do this: flipping the box swaps front for back, and the media pump
        /// replaces a game's placeholder with its real artwork once it hydrates. In both cases
        /// the game may be on screen right now, so the replacement is decoded immediately
        /// rather than waiting for the next navigation to warm it.
        /// </summary>
        public void Invalidate(GameFiles gameFiles)
        {
            if (gameFiles == null)
            {
                return;
            }

            bool wasDecoded;

            lock (cacheLock)
            {
                LinkedListNode<GameFiles> existing = decoded.Find(gameFiles);
                wasDecoded = existing != null;

                if (wasDecoded)
                {
                    decoded.Remove(existing);
                }
            }

            // Nothing was decoded for this game, so there is nothing stale to replace. This is
            // the common case by far: every game in the library passes through here twice while
            // the catalog is built, long before any of them are on screen, and re-decoding on
            // that path would decode the whole library at startup.
            if (!wasDecoded)
            {
                return;
            }

            gameFiles.FrontImageSource = null;
            Warm(gameFiles);
        }

        #region singleton implementation

        public static RowImageDecoder Instance => instance;

        private static readonly RowImageDecoder instance = new RowImageDecoder();

        static RowImageDecoder()
        {
        }

        private RowImageDecoder()
        {
        }

        #endregion
    }
}
