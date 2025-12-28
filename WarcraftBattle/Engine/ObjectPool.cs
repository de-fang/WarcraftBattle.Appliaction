using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WarcraftBattle.Engine
{
    public class ObjectPool<T> where T : new()
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator = null)
        {
            _objectGenerator = objectGenerator ?? (() => new T());
            _objects = new ConcurrentBag<T>();
        }

        public T Get()
        {
            if (_objects.TryTake(out T item)) return item;
            return _objectGenerator();
        }

        public void Return(T item)
        {
            _objects.Add(item);
        }
    }
}
