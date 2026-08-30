using Eclipse.Models;

namespace Eclipse.Service
{
    /// <summary>Which of the five bezel levels a video should be framed with.</summary>
    public enum BezelSource
    {
        /// <summary>No bezel - the video fills the frame.</summary>
        None,

        /// <summary>The bezel already resolved for this game: levels 1-3.</summary>
        GameSpecific,

        /// <summary>A default for the orientation: the platform's, then the global one.</summary>
        Default
    }

    /// <summary>
    /// What to frame a video with. <see cref="Orientation"/> is only meaningful when
    /// <see cref="Source"/> is <see cref="BezelSource.Default"/>.
    /// </summary>
    public struct BezelSelection
    {
        public BezelSelection(BezelSource source, BezelOrientation orientation)
        {
            Source = source;
            Orientation = orientation;
        }

        public BezelSource Source { get; }

        public BezelOrientation Orientation { get; }
    }

    /// <summary>
    /// Which bezel a video should get, given what was already resolved for the game and how
    /// large the video turned out to be.
    ///
    /// This is separate from <see cref="BezelService"/> on purpose. The service owns a
    /// dictionary built from the filesystem in its constructor, and it asks LaunchBox for the
    /// platform list to build it, so it cannot be constructed in a test. The *rules* have no
    /// such dependency, and they are the part with behaviour worth pinning - the widescreen
    /// cutoff, the orientation, and which level beats which.
    ///
    /// See RULE-MEDIA-020 to RULE-MEDIA-027 in docs/features/media.md.
    /// </summary>
    public static class BezelRules
    {
        /// <summary>
        /// At this aspect ratio or wider, a video is treated as widescreen and gets no default
        /// bezel. Deliberately a double, and deliberately compared against a float ratio - see
        /// the note in <see cref="Choose"/>.
        /// </summary>
        public const double WidescreenAspectRatio = 1.7;

        /// <param name="hasGameBezel">Whether levels 1-3 already found a bezel for this game.</param>
        /// <param name="videoWidth">The video's natural width, or 0 if it is not known yet.</param>
        /// <param name="videoHeight">The video's natural height, or 0 if it is not known yet.</param>
        public static BezelSelection Choose(bool hasGameBezel, int videoWidth, int videoHeight)
        {
            // A bezel chosen for this particular game wins outright, whatever shape the video
            // is. The widescreen cutoff below has never applied to it: in the code this
            // replaces, the ratio test sat inside the "no game bezel" branch. RULE-MEDIA-024
            // used to read as though it applied to every bezel.
            if (hasGameBezel)
            {
                return new BezelSelection(BezelSource.GameSpecific, BezelOrientation.Horizontal);
            }

            // Nothing is known about the video yet, so there is no orientation to choose and
            // no ratio to test. A default bezel picked now could be the wrong one.
            if (videoHeight == 0)
            {
                return new BezelSelection(BezelSource.None, BezelOrientation.Horizontal);
            }

            // Computed in float and then compared against a double, which is what the original
            // expression did - `(float)(width / (float)height) < 1.7`. Widening the float to
            // double for the comparison is what decides the borderline cases, so the shape of
            // this expression is preserved rather than tidied into double arithmetic.
            float aspectRatio = videoWidth / (float)videoHeight;

            if (aspectRatio >= WidescreenAspectRatio)
            {
                return new BezelSelection(BezelSource.None, BezelOrientation.Horizontal);
            }

            // Taller than it is wide gets the vertical bezel; square counts as horizontal.
            BezelOrientation orientation = videoWidth < videoHeight
                ? BezelOrientation.Vertical
                : BezelOrientation.Horizontal;

            return new BezelSelection(BezelSource.Default, orientation);
        }
    }
}
