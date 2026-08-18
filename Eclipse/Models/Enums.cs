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
