using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder.DataTranslation
{
    public class ObjectLoader
    {
        private readonly Type type;
        public readonly string[] PrimaryKeysName;
        private readonly TranslationConfig config;
        private readonly BindingFlags PropertiesFlags = BindingFlags.Instance | BindingFlags.Public;
        public readonly Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
        private readonly Dictionary<string, ISqlTranslation> conversors = new Dictionary<string, ISqlTranslation>();

        public string Name { get; }

        public ObjectLoader(Type type, TranslationConfig config)
        {
            this.type = type;
            this.config = config;
            this.LoadProperties();
            this.Name = type.GetCustomAttribute<TableAttribute>(false)?.Name ?? type.Name;
            this.PrimaryKeysName = GetAttrPrimaryKeys().Select(pk => GetColumnName(pk)).ToArray();
        }

        public IEnumerable<PropertyInfo> GetAttrPrimaryKeys()
        {
            var props = type.GetProperties(PropertiesFlags)
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (props.Any())
                return props;

            if (type.GetProperty("Id", PropertiesFlags) is PropertyInfo idProperty)
                return new[] { idProperty };

            return new PropertyInfo[0];
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
            return this.PrimaryKeysName.Contains(column);
        }

        private bool IsInvalidPk(object value)
        {
            return value is null || value is DBNull || value is int intVal && intVal == 0;
        }

        private void LoadProperties()
        {
            foreach (var prop in type.GetProperties(PropertiesFlags))
            {
                if (prop.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;

                string column = GetColumnName(prop);
                this.Properties[column] = prop;

                if (this.config.GetOf(prop) is ISqlTranslation conversor)
                    this.conversors[column] = conversor;
            }
        }

        public object GetColumnValue(string column, object owner, PropertyInfo property)
        {
            object value = property.GetValue(owner);
            if (this.conversors.TryGetValue(column, out var conversor) && conversor != null)
                value = conversor.ToSqlValue(value, property.DeclaringType);

            try
            {
                return this.config.ToSql(value);
            }
            catch (NotSupportedException)
            {
                throw new NotSupportedException($"Type \"{type.FullName}\" of Column \"{GetColumnName(property)}\" is not supported");
            }
        }

        public void SetColumnValue(object owner, PropertyInfo property, object value)
        {
            string name = GetColumnName(property);

            try
            {
                if (this.conversors.TryGetValue(name, out var conversor) && conversor != null)
                    value = conversor.FromSqlValue(value, property.DeclaringType);
                else
                    value = this.config.FromSql(value, property.PropertyType);

                property.SetValue(owner, value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fail on set column \"{name}\" value.", ex);
            }
        }

        public static string GetColumnName(PropertyInfo property)
        {
            if (property.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute col)
                return col.Name;

            return property.Name;
        }
    }
}
