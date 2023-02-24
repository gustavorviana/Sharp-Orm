using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    public class ObjectTranslator : IObjectTranslator
    {
        private readonly Dictionary<Type, ObjectLoader> cachedLoaders = new Dictionary<Type, ObjectLoader>();
        private readonly object lockObj = new object();

        public TranslationConfig Config { get; }

        public ObjectTranslator(TranslationConfig config)
        {
            this.Config = config;
        }

        public T ParseFromReader<T>(DbDataReader reader) where T : new()
        {
            if (typeof(T) == typeof(Row))
                return (T)(object)reader.GetRow();

            T obj = new T();
            if (obj is Model model)
            {
                model.LoadFromDataReader(reader);
                return obj;
            }

            var loader = this.GetLoader(typeof(T));
            for (int i = 0; i < reader.FieldCount; i++)
                if (loader.TryGetProperty(reader.GetName(i), out var property))
                    loader.SetColumnValue(obj, property, reader[i]);

            return obj;
        }

        public Row ToRow(object obj, Type type)
        {
            if (obj is Row row)
                return row;

            if (obj is Model model)
                return new Row(model.GetCells());

            return new Row(this.GetLoader(type).GetCells(obj).ToArray());
        }

        public string GetTableNameOf(Type type)
        {
            return GetLoader(type).Name;
        }

        public string[] GetPrimatyKey(Type type)
        {
            return this.GetLoader(type).PrimaryKeysName;
        }

        public ObjectLoader GetLoader(Type type)
        {
            lock (lockObj)
            {
                if (this.cachedLoaders.TryGetValue(type, out var loader))
                    return loader;

                this.cachedLoaders.Add(type, loader = new ObjectLoader(type, this.Config));
                return loader;
            }
        }
    }
}
