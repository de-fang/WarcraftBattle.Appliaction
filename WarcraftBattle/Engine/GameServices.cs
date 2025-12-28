using System;
using System.Collections.Generic;

namespace WarcraftBattle.Engine
{
    public static class GameServices
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            _services[typeof(T)] = service;
        }

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            return default(T);
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}