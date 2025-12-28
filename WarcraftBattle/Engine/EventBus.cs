using System;
using System.Collections.Generic;

namespace WarcraftBattle.Engine
{
    public static class EventBus
    {
        private static Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<object>();
            }
            _subscribers[type].Add(handler);
        }

        public static void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
            {
                // Create a copy to allow modification during iteration if needed (though typically not recommended)
                // or just iterate.
                foreach (var handlerObj in _subscribers[type])
                {
                    if (handlerObj is Action<T> handler)
                    {
                        handler(eventData);
                    }
                }
            }
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
            {
                _subscribers[type].Remove(handler);
            }
        }
    }
}
