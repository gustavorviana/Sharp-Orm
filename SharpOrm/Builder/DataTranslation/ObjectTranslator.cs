using SharpOrm.Errors;
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
        static ObjectTranslator()
        {
            if (AnnotationsAssemblyRedirector.NeedLoad())
                AnnotationsAssemblyRedirector.LoadRedirector();
        }

        private readonly Dictionary<Type, ObjectLoader> cachedLoaders = new Dictionary<Type, ObjectLoader>();

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

        public static IEnumerable<PropertyInfo> GetPrimaryKeyOfType(Type type)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (props.Any())
                return props;

            if (type.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public) is PropertyInfo idProperty)
                return new[] { idProperty };

            return Array.Empty<PropertyInfo>();
        }

        public static SqlValueConversor GetConversorOf(PropertyInfo property)
        {
            if (property.GetCustomAttribute<SqlConverterAttribute>() is SqlConverterAttribute attribute)
                return (SqlValueConversor)Activator.CreateInstance(attribute.Type);

            return null;
        }

        public static object ToSqlValue(object value, string notSupportedMessage)
        {
            if (value == null || value is DBNull)
                return DBNull.Value;

            Type type = value.GetType();

            if (type.IsEnum)
                return Convert.ToInt32(value);

            if (value is DateTime date)
                return date == DateTime.MinValue ? DBNull.Value : value;

            if (value is TimeSpan || value is string || type.IsPrimitive)
                return value;

            throw new NotSupportedException(notSupportedMessage);
        }

        protected static object FromSqlValue(object value)
        {
            if (value is DBNull)
                return null;

            return value;
        }

        protected internal class ObjectLoader
        {
            private readonly Type type;
            private readonly string[] primaryKeys;
            public readonly Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
            private readonly Dictionary<string, SqlValueConversor> conversors = new Dictionary<string, SqlValueConversor>();

            public ObjectLoader(Type type)
            {
                this.type = type;
                this.LoadProperties();
                this.primaryKeys = GetPrimaryKeyOfType(type).Select(pk => GetColumnName(pk)).ToArray();
            }

            public IEnumerable<Cell> GetCells(object owner)
            {
                foreach (var item in this.Properties)
                {
                    object value = this.GetColumnValue(item.Key, owner, item.Value);
                    if (this.IsPrimaryKey(item.Key) && this.IsInvalidPk(value))
                        continue;

                    yield return new Cell(item.Key, value);
                }
            }

            private bool IsPrimaryKey(string column)
            {
                return this.primaryKeys.Contains(column);
            }

            private bool IsInvalidPk(object value)
            {
                return value is null || value is DBNull || value is int intVal && intVal == 0;
            }

            private void LoadProperties()
            {
                foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.GetCustomAttribute<NotMappedAttribute>() != null)
                        continue;

                    string column = GetColumnName(prop);
                    this.Properties[column] = prop;

                    if (GetConversorOf(prop) is SqlValueConversor conversor)
                        this.conversors[column] = conversor;
                }
            }

            public object GetColumnValue(string column, object owner, PropertyInfo property)
            {
                object value = property.GetValue(owner);
                if (this.conversors.TryGetValue(column, out var conversor) && conversor != null)
                    value = conversor.ToSqlValue(value, property.DeclaringType, column);

                return ToSqlValue(value, $"Type of Column \"{GetColumnName(property)}\" is not supported");
            }

            public void SetColumnValue(object owner, PropertyInfo property, object value)
            {
                value = FromSqlValue(value);
                if (CanUpdateValue(property, value))
                    property.SetValue(owner, this.LoadValueFromDb(property, value));
            }

            private bool CanUpdateValue(PropertyInfo property, object value)
            {
                return value == null ||
                    property.PropertyType.IsValueType ||
                    property.PropertyType == typeof(Nullable<>) ||
                    property.PropertyType.IsPrimitive ||
                    property.PropertyType == typeof(string);
            }

            private object LoadValueFromDb(PropertyInfo property, object value)
            {
                string name = GetColumnName(property);
                if (this.conversors.TryGetValue(name, out var conversor) && conversor != null)
                    return conversor.FromSqlValue(value, property.DeclaringType, name);

                if (property.PropertyType == typeof(DateTime) && value == null)
                    return DateTime.MinValue;

                if (property.PropertyType.IsEnum)
                    return Enum.ToObject(property.PropertyType, value);

                return value;
            }
        }
    }
}
