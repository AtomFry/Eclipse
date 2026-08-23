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

    // How far a voice search has got, and therefore what is on screen.
    //
    // This replaces a bool that every way out of VoiceRecognitionState had to reset by hand -
    // nine assignments, one per exit path, with nothing to say that a tenth exit would need a
    // tenth reset. Entering and leaving are one call each on the view model now.
    //
    // Two phases exist because two are visible. Recognition finishing and the results being
    // assembled happen in one synchronous block, so a Matching phase would never be rendered;
    // it belongs here only if that ever stops being true.
    public enum VoiceSearchPhase
    {
        // No voice search is running.
        Inactive,

        // Listening for an utterance, with the indicator on screen.
        Listening
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
