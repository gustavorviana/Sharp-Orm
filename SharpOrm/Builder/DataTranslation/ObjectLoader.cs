﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Class responsible for loading objects and their properties for use in database translation.
    /// </summary>
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

        /// <summary>
        /// Gets the primary key properties of a type based on the presence of the [Key] attribute or a property named "Id".
        /// </summary>
        /// <returns>An enumerable of the primary key properties of the type.</returns>
        public IEnumerable<PropertyInfo> GetAttrPrimaryKeys()
        {
            var props = type.GetProperties(PropertiesFlags)
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
                .OrderBy(p => GetColumnOrder(p));

            if (props.Any())
                return props;

            if (type.GetProperty("Id", PropertiesFlags) is PropertyInfo idProperty)
                return new[] { idProperty };

            return new PropertyInfo[0];
        }

        private static int GetColumnOrder(PropertyInfo property)
        {
            if (property.GetCustomAttribute<ColumnAttribute>()  is ColumnAttribute attr)
                return attr.Order;

            return int.MaxValue;
        }

        /// <summary>
        /// Gets the cells of an object's properties for use in database translation.
        /// </summary>
        /// <param name="owner">The object to get the cells from.</param>
        /// <param name="ignorePrimaryKey">Whether to ignore primary key columns when getting the cells.</param>
        /// <returns>An enumerable of the cells of the object's properties.</returns>
        public IEnumerable<Cell> GetCells(object owner, bool ignorePrimaryKey = false)
        {
            foreach (var item in this.Properties)
            {
                object value = this.GetColumnValue(item.Key, owner, item.Value);
                if (this.IsPrimaryKey(item.Key) && (ignorePrimaryKey || TranslationUtils.IsInvalidPk(value)))
                    continue;

                yield return new Cell(item.Key, value);
            }
        }

        private bool IsPrimaryKey(string column)
        {
            return this.PrimaryKeysName.Contains(column);
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

        public object GetColumnValue(string column, object owner)
        {
            return this.GetColumnValue(column, owner, this.Properties[column]);
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

        public bool HasConversor(string columnName)
        {
            return this.conversors.ContainsKey(columnName.ToLower());
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

        public static bool IsNative(Type type)
        {
            return NativeSqlValueConversor.IsNative(type);
        }

        public static bool IsRequired(PropertyInfo property)
        {
            return property.GetCustomAttribute<RequiredAttribute>() != null;
        }

        public static string GetColumnName(PropertyInfo property, bool usePropertyNameIfEmpty = true)
        {
            if (property.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute col && !string.IsNullOrEmpty(col.Name))
                return col.Name;

            return usePropertyNameIfEmpty ? property.Name : null;
        }

        public bool TryGetProperty(string name, out PropertyInfo property)
        {
            foreach (var item in this.Properties)
            {
                if (!item.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;

                property = item.Value;
                return true;
            }

            property = null;
            return false;
        }
    }
}
