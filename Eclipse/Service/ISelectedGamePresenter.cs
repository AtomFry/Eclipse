using Eclipse.Models;

namespace Eclipse.Service
{
    /// <summary>
    /// What the selected-game sequence needs the screen to do, and nothing else.
    ///
    /// The sequence used to live in <c>MainWindowView</c>, where every named element in the
    /// theme was one identifier away and the order of the steps could only be checked by
    /// watching it happen. It depends on this instead, so the surface it can touch is these
    /// nine members - and the ordering can be asserted against a fake.
    ///
    /// This is the same shape as <see cref="IAttractModePresenter"/>, deliberately. The two
    /// sequences are siblings and should read like it.
    ///
    /// Every member is called on the UI thread.
    /// </summary>
    public interface ISelectedGamePresenter
    {
        /// <summary>
        /// Dim the outgoing game - background, clear logo, title and details - so the change is
        /// acknowledged immediately, before anything is loaded (RULE-PRESENT-002).
        /// </summary>
        void DimForGameChange();

        /// <summary>
        /// Stop playback and the check on whether it started. Called when the selection moves
        /// and when a game is launching; not a fade, just a halt.
        /// </summary>
        void StopPlayback();

        /// <summary>
        /// Hand a file to the player, or clear it when the path is null or empty. Assigning a
        /// source does not begin playback.
        /// </summary>
        void OpenVideo(string videoPath);

        /// <summary>
        /// Whether the player has a video it is willing to play: false when previews are
        /// disabled, when they have been abandoned for the session after repeated failures, or
        /// when the settled game simply has no video. Only meaningful after
        /// <see cref="OpenVideo"/>.
        /// </summary>
        bool CanPlayVideo { get; }

        /// <summary>
        /// Put the settled game's artwork and text on screen and fade them in. The presenter
        /// remembers what it was given, so <see cref="StopAndRestore"/> can put it back.
        /// </summary>
        void ShowGameDetails(DecodedGameMedia media);

        /// <summary>Fade the new background in over the outgoing game's dimmed one.</summary>
        void FadeInBackground();

        /// <summary>
        /// The fade-in has finished: hand the picture to the displayed layer at full opacity and
        /// hide the layer that was fading. Both are showing the same picture, so the handover
        /// itself is invisible - what matters is that the background stops being dim.
        /// </summary>
        void SettleBackground();

        /// <summary>
        /// Start playback and begin fading the background out behind it.
        /// </summary>
        void PlayVideoAndFadeOutBackground();

        /// <summary>
        /// The fade-out has finished: the displayed layer takes the active layer's picture and
        /// the active layer is hidden.
        /// </summary>
        void SwapBackgroundLayers();

        /// <summary>
        /// Put the selected game's artwork back on screen at full opacity. Called when a game is
        /// launching or voice recognition starts, after the sequence has been stopped.
        /// </summary>
        void StopAndRestore();
    }
}
