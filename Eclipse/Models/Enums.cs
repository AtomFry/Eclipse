using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Models
{
    public enum TitleMatchType
    {
        FullTitleMatch = 100,
        MainTitleMatch = 95,
        SubtitleMatch = 90,
        FullTitleContains = 60,
        None = 0
    }

    // Whether a voice search can be started right now. The index and the recogniser are
    // built in the background after startup, so there is a window where the feature exists
    // but is not usable yet.
    public enum VoiceSearchAvailability
    {
        Disabled,
        Preparing,
        Ready,
        Failed
    }

    // How a recognition attempt ended.
    //
    // This replaces a Cancelled bool beside an ErrorMessage string, where "heard nothing" and
    // "the engine faulted" were the same field holding different words - so the only way to tell
    // an ordinary outcome from a real failure was to read the message, and the recogniser had to
    // compose text for the screen to make that possible.
    public enum SpeechRecognitionOutcome
    {
        // The recogniser finished. RecognizedPhrases holds what it heard, which may be nothing
        // that matches anything.
        Completed,

        // Silence, or noise nothing could be made out of. Ordinary, and the user fixes it by
        // speaking again.
        HeardNothing,

        // The engine or the audio device failed. Not something talking louder will solve; the
        // detail is in the log.
        Failed,

        // The caller gave up. Nothing to search for, and it must not be mistaken for a search
        // that returned nothing.
        Cancelled
    }

    // How far a voice search has got, and therefore what is on screen.
    //
    // This replaces a bool that every way out of VoiceRecognitionState had to reset by hand -
    // nine assignments, one per exit path, with nothing to say that a tenth exit would need a
    // tenth reset. Entering and leaving are one call each on the view model now.
    //
    // Recognition finishing and the results being assembled happen in one synchronous block, so
    // a Matching phase would never be rendered; it belongs here only if that ever stops being
    // true.
    public enum VoiceSearchPhase
    {
        // No voice search is running.
        Inactive,

        // Listening for an utterance, with the indicator on screen.
        Listening,

        // Heard something, matched nothing. The notice stands in the list heading and the lists
        // are left exactly as they were, so this is a phase the user browses straight out of
        // rather than a screen they have to dismiss.
        NoMatch
    }

    public enum BezelType
    {
        Game,
        PlatformDefault,
        Default
    }

    public enum BezelOrientation
    {
        Horizontal,
        Vertical
    }

    public enum FeatureGameOption
    {
        PlayGame,
        MoreInfo
    }

    public enum FilterFieldOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        IsNull,
        IsNotNull,
        Contains
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public enum PageFunction
    {
        PageUp,
        PageDown,
        VoiceSearch,
        RandomGame,
        FlipBox,
        ZoomBox,
        VolumeUp,
        VolumeDown,
        DisplayDetails,
        PlayGame
    }
}
