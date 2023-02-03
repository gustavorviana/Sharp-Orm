using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    public class ObjectTranslator : IObjectTranslator
    {
        static ObjectTranslator()
        {
            if (AnnotationsAssemblyRedirector.NeedLoad())
                AnnotationsAssemblyRedirector.LoadRedirector();
        }

        private readonly Dictionary<Type, ObjectLoader> cachedLoaders = new Dictionary<Type, ObjectLoader>();

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

            foreach (var property in loader.Properties)
            {
                int colIndex = reader.GetOrdinal(property.Key);
                if (colIndex < 0)
                    throw new DatabaseException($"The column \"{property.Key}\" not found in database");

                loader.SetColumnValue(obj, property.Value, reader[property.Key]);
            }

            return obj;
        }

        public Row ToRow(object obj)
        {
            if (obj is Row row)
                return row;

            if (obj is Model model)
                return new Row(model.GetCells());

            return new Row(this.GetLoader(obj.GetType()).GetCells(obj).ToArray());
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
            if (this.cachedLoaders.TryGetValue(type, out var loader))
                return loader;

            this.cachedLoaders.Add(type, loader = new ObjectLoader(type, this.Config));
            return loader;
        }

    }
}
