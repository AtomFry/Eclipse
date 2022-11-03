using Prism.Events;

namespace Eclipse.Event
{
    public class EclipseSettingsListClose : PubSubEvent { }
    public class InitializedEvent : PubSubEvent { }
    public class UpEvent : PubSubEvent { }
    public class DownEvent : PubSubEvent { }
    public class LeftEvent : PubSubEvent { }
    public class RightEvent : PubSubEvent { }
    public class PageUpEvent : PubSubEvent { }
    public class PageDownEvent : PubSubEvent { }
    public class EnterEvent : PubSubEvent { }
    public class EscEvent : PubSubEvent { }
    public class VideoStartedEvent : PubSubEvent { }
    public class VideoEndedEvent : PubSubEvent { }
    public class GameStartedEvent : PubSubEvent { }
    public class GameEndedEvent : PubSubEvent { }

    // events for eclipse settings views
    public class EclipseSettingsClose : PubSubEvent { }
    public class CustomListDefinitionSaved : PubSubEvent<string> { }
    public class CustomListDefinitionEditClosing : PubSubEvent { }
    public class CustomListDefinitionEditClose : PubSubEvent { }
}
