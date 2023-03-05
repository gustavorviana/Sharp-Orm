using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Reflection;

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
            return (T)this.ParseFromReader(typeof(T), reader, "");
        }

        private object ParseFromReader(Type typeToParse, DbDataReader reader, string prefix)
        {
            if (typeToParse == typeof(Row))
                return reader.GetRow();

            object obj = Activator.CreateInstance(typeToParse);
            if (obj is Model model)
            {
                model.LoadFromDataReader(reader);
                return obj;
            }

            var loader = this.GetLoader(typeToParse);
            foreach (var property in loader.Properties.Values)
                this.LoadPropertyValue(obj, loader, reader, property, prefix);

            return obj;
        }

        private void LoadPropertyValue(object obj, ObjectLoader loader, DbDataReader reader, PropertyInfo property, string fullName)
        {
            string column = ObjectLoader.GetColumnName(property, false);
            if (!string.IsNullOrEmpty(fullName) && string.IsNullOrEmpty(column))
                fullName += "_" + property.Name;

            bool isNative = ObjectLoader.IsNative(property.PropertyType);
            string validName = new[] { column, fullName, property.Name }.FirstOrDefault(x => !string.IsNullOrEmpty(x));
            int index = reader.GetIndexOf(validName);
            if ((!isNative || index == -1) && !ObjectLoader.IsRequired(property))
                return;

            if (isNative || loader.HasConversor(column ?? property.Name))
            {
                if (index < 0)
                    throw new KeyNotFoundException($"Could not find column in database with key {validName}, failed to load value for {property.DeclaringType.FullName}.{property.Name}.");

                loader.SetColumnValue(obj, property, reader[index]);
                return;
            }

            if (string.IsNullOrEmpty(fullName))
                fullName = column ?? property.Name;

            property.SetValue(obj, this.ParseFromReader(property.PropertyType, reader, fullName));
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
