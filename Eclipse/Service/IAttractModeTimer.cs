namespace Eclipse.Service
{
    /// <summary>
    /// The idle countdown that starts the screen saver, as the selected-game sequence needs it.
    ///
    /// Exists so <see cref="SelectedGameSequence"/> does not depend on
    /// <see cref="AttractModeService"/>, which is a singleton holding a timer and a view model
    /// and cannot be constructed in a test. These two calls are the whole of what the sequence
    /// does to it.
    /// </summary>
    public interface IAttractModeTimer
    {
        /// <summary>
        /// Stop the countdown and any slideshow already running - something is about to be on
        /// screen that the screen saver must not interrupt.
        /// </summary>
        void StopAttractMode();

        /// <summary>Restart the countdown from zero.</summary>
        void RestartAttractMode();
    }
}
