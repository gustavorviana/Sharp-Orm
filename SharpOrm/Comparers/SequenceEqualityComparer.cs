using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Comparers
{
    public class SequenceEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        private readonly IEqualityComparer<T> _itemComparer;

        public static readonly SequenceEqualityComparer<T> Default = new SequenceEqualityComparer<T>();

        public SequenceEqualityComparer(IEqualityComparer<T> itemComparer = null)
        {
            _itemComparer = itemComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x == null || y == null)
                return false;

            return x.SequenceEqual(y, _itemComparer);
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            if (obj == null)
                return 0;

            unchecked
            {
                int hash = 17;
                foreach (var item in obj)
                {
                    hash = hash * 31 + (item != null ? _itemComparer.GetHashCode(item) : 0);
                }
                return hash;
            }
        }
    }
}
