﻿using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder.DataTranslation
{
    public class ObjectTranslator : IObjectTranslator
    {
        private readonly Dictionary<Type, ObjectLoader> cachedLoaders = new Dictionary<Type, ObjectLoader>();

        public T ParseFromReader<T>(DbDataReader reader) where T : new()
        {
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
            if (obj is Model model)
                return new Row(model.GetCells());

            return new Row(this.GetLoader(obj.GetType()).GetCells(obj));
        }

        private ObjectLoader GetLoader(Type type)
        {
            if (this.cachedLoaders.TryGetValue(type, out var loader))
                return loader;

            this.cachedLoaders.Add(type, loader = new ObjectLoader(type));
            return loader;
        }

        public static string GetTableNameOf(Type type)
        {
            return type.GetCustomAttribute<TableAttribute>(false)?.Name ?? type.Name;
        }

        public static string GetColumnName(PropertyInfo property)
        {
            if (property.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute col)
                return col.Name;

            return property.Name;
        }

        public static PropertyInfo GetPrimaryKeyOfType(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
        }

        protected internal class ObjectLoader
        {
            private readonly Type type;
            public readonly Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();

            public ObjectLoader(Type type)
            {
                this.type = type;
                this.LoadProperties();
            }

            public Cell[] GetCells(object owner)
            {
                return this.Properties.Select(kvp => new Cell(kvp.Key, this.GetColumnValue(owner, kvp.Value))).ToArray();
            }

            private void LoadProperties()
            {
                foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    if (prop.GetCustomAttribute<NotMappedAttribute>() == null)
                        this.Properties[GetColumnName(prop)] = prop;
            }

            public object GetColumnValue(object owner, PropertyInfo property)
            {
                object value = property.GetValue(owner);
                if (value == null)
                    return DBNull.Value;

                Type type = value.GetType();

                if (type.IsEnum)
                    return Convert.ToInt32(value);

                if (value is DateTime || value is TimeSpan || value is DBNull || type.IsPrimitive)
                    return value;

                object objPk = GetPrimaryKeyOfType(type);
                if (objPk?.GetType()?.IsPrimitive ?? false)
                    return objPk;

                throw new NotSupportedException($"Column type \"{GetColumnName(property)}\" is not supported");
            }

            public void SetColumnValue(object owner, PropertyInfo property, object value)
            {
                if (CanUpdateValue(property, value))
                    property.SetValue(owner, this.LoadValueForColumn(property, value));
            }

            private bool CanUpdateValue(PropertyInfo property, object value)
            {
                return (!(value is DBNull) && value != null) || !property.PropertyType.IsValueType || property.PropertyType == typeof(Nullable<>);
            }

            private object LoadValueForColumn(PropertyInfo property, object value)
            {
                if (value == null || value is DBNull)
                    return null;

                if (property.PropertyType.IsEnum)
                    return Enum.ToObject(property.PropertyType, value);

                return value;
            }
        }
    }
}
