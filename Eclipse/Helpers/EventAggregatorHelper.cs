using Prism.Events;

namespace Eclipse.Helpers
{
    public sealed class EventAggregatorHelper
    {
        private static readonly EventAggregatorHelper instance = new EventAggregatorHelper();

        static EventAggregatorHelper()
        {
        }

        private EventAggregatorHelper()
        {
            EventAggregator = new EventAggregator();
        }

        public static EventAggregatorHelper Instance => instance;

        public EventAggregator EventAggregator { get; }
    }
}