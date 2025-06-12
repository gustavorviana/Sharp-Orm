using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Interceptors
{
    /// <summary>
    /// Context information for model interceptor operations
    /// </summary>
    public class ModelInterceptorContext
    {
        private readonly List<DbEntry> _entries = new List<DbEntry>();
        private readonly Query _query;
        internal bool _hasChange;

        public ConnectionManager Connection => _query.Manager;
        public QueryConfig Config => _query.Config;

        public ModelInterceptorContext(Query query)
        {
            _query = query;
        }

        public IEnumerable<DbEntry> Entries() => _entries;

        public IEnumerable<DbEntry<T>> Entries<T>() => _entries.OfType<DbEntry<T>>();

        internal ModelInterceptorContext AddObjects<T>(ObjectReader reader, TableInfo info, IEnumerable<T> objects, EntryState state)
        {
            _entries.AddRange(objects.Select(obj => new DbEntry<T>(reader, info, obj, state)));
            return this;
        }

        internal ModelInterceptorContext AddObject<T>(ObjectReader reader, TableInfo info, T obj, EntryState state)
        {
            _entries.Add(new DbEntry<T>(reader, info, obj, state));
            return this;
        }
    }
}
