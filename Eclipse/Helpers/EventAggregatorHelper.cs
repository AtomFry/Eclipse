﻿using Prism.Events;

namespace Eclipse.Helpers
{
    public sealed class EventAggregatorHelper
    {
        private static readonly EventAggregatorHelper instance = new EventAggregatorHelper();

        static EventAggregatorHelper()
        {
        }

        private EventAggregatorHelper() => eventAggregator = new EventAggregator();

        public static EventAggregatorHelper Instance => instance;

        private EventAggregator eventAggregator;

        public EventAggregator EventAggregator => eventAggregator;
    }
}
