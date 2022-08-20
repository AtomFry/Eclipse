using Eclipse.Models;
using Prism.Events;

namespace Eclipse.Event
{
    public class GameSelectedEvent : PubSubEvent { }
    public class StopEverythingEvent : PubSubEvent { }
    public class VideoStartedEvent : PubSubEvent { }
    public class VideoEndedEvent : PubSubEvent { }
}
