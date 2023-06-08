using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.DataTranslation
{
    public abstract class TableTranslatorBase
    {
        private readonly ConcurrentDictionary<Type, TableInfo> cachedTables = new ConcurrentDictionary<Type, TableInfo>();

        public TranslationRegistry Registry { get; }

        public TableTranslatorBase(TranslationRegistry registry)
        {
            this.Registry = registry;
        }

        public T ParseFromReader<T>(DbDataReader reader) where T : new()
        {
            if (typeof (T) == typeof(Row))
                return (T)(object)reader.GetRow(this.Registry);

            return (T)this.ParseFromReader(typeof(T), reader, "");
        }

        protected abstract object ParseFromReader(Type typeToParse, DbDataReader reader, string prefix);

        public Row ToRow(object obj, Type type)
        {
            if (obj is Row row)
                return row;

            return new Row(this.GetLoader(type).GetCells(obj).ToArray());
        }

        public string GetTableNameOf(Type type)
        {
            return GetLoader(type).Name;
        }

        public TableInfo GetLoader(Type type)
        {
            return this.cachedTables.GetOrAdd(type, _type => new TableInfo(this.Registry, _type));
        }
    }
}
