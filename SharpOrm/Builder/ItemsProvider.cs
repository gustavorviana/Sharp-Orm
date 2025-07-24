using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    public class ItemsProvider<V> : ItemsProvider<Type, V> where V : ICanWork<Type>
    {

    }

    public class ItemsProvider<K, V> : ICollection<V> where V : ICanWork<K>
    {
        protected ConcurrentBag<V> Bag { get; } = new ConcurrentBag<V>();
        protected ConcurrentDictionary<K, V> _cached { get; } = new ConcurrentDictionary<K, V>();

        int ICollection<V>.Count => Bag.Count;

        bool ICollection<V>.IsReadOnly => false;

        public void Add(V item) => Bag.Add(item);

        public V Get(K key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _cached.GetOrAdd(key, _key => Bag.FirstOrDefault(x => x.CanWork(_key)));
        }

        protected bool TryAdd(K key, Func<V> func)
        {
            if (_cached.ContainsKey(key))
                return false;

            var value = func();
            if (!_cached.TryAdd(key, value))
                return false;

            Bag.Add(value);
            return true;
        }

        void ICollection<V>.Clear()
        {

        }

        bool ICollection<V>.Contains(V item)
            => Bag.Contains(item);


        void ICollection<V>.CopyTo(V[] array, int arrayIndex)
            => Bag.CopyTo(array, arrayIndex);

        IEnumerator<V> IEnumerable<V>.GetEnumerator()
            => Bag.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Bag.GetEnumerator();

        bool ICollection<V>.Remove(V item)
        {
            return false;
        }
    }
}
