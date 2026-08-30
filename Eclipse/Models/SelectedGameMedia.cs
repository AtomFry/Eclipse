using System;
using System.Windows.Media;

namespace Eclipse.Models
{
    /// <summary>
    /// Where a game's media lives, and the text that goes beside it - read off the view model
    /// the moment the selection changes.
    ///
    /// Reading paths and strings is free, so it happens immediately. Decoding the five images
    /// is not, so it waits for the selection to settle and then happens off the UI thread - see
    /// <see cref="DecodedGameMedia"/>. Capturing early is what lets the sequence be cancelled
    /// without having read a single file for a game the user scrolled straight past.
    /// </summary>
    public sealed class SelectedGameMedia
    {
        public Uri Background { get; set; }
        public Uri ClearLogo { get; set; }
        public Uri PlayMode { get; set; }
        public Uri PlatformLogo { get; set; }
        public Uri GameBezel { get; set; }

        /// <summary>The video file for this game, or null if it has none.</summary>
        public string VideoPath { get; set; }

        public string Title { get; set; }
        public string MatchDescription { get; set; }
        public string ReleaseYear { get; set; }
    }

    /// <summary>
    /// The same game's media, decoded and frozen, ready to be put on screen. Every image may be
    /// null - a game with no clear logo, or a file that failed to load.
    /// </summary>
    public sealed class DecodedGameMedia
    {
        public ImageSource Background { get; set; }
        public ImageSource ClearLogo { get; set; }
        public ImageSource PlayMode { get; set; }
        public ImageSource PlatformLogo { get; set; }
        public ImageSource GameBezel { get; set; }

        public string Title { get; set; }
        public string MatchDescription { get; set; }
        public string ReleaseYear { get; set; }
    }
}
