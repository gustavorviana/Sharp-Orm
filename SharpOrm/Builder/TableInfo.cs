using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents information about a database table.
    /// </summary>
    public class TableInfo
    {
        private readonly BindingFlags propertiesFlags = BindingFlags.Instance | BindingFlags.Public;
        public Type Type { get; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets an array of column information for the table.
        /// </summary>
        public ColumnInfo[] Columns { get; }

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation configuration and type.
        /// </summary>
        /// <param name="config">The translation configuration.</param>
        /// <param name="type">The type representing the table.</param>
        public TableInfo(TranslationRegistry config, Type type)
        {
            if (type == null || type.IsAbstract || type == typeof(Row))
                throw new InvalidOperationException($"Invalid type provided for the {nameof(TableInfo)} class.");

            this.Type = type;
            this.Name = type.GetCustomAttribute<TableAttribute>(false)?.Name ?? type.Name;
            this.Columns = this.GetColumns(config).Distinct().ToArray();
        }

        private IEnumerable<ColumnInfo> GetColumns(TranslationRegistry registry)
        {
            foreach (var prop in Type.GetProperties(propertiesFlags))
                if (prop.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, prop);

            foreach (var field in Type.GetFields(propertiesFlags))
                if (field.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, field);

            if (GetByName(registry, "Id") is ColumnInfo pk)
                yield return pk;
        }

        public ColumnInfo GetByName(TranslationRegistry registry, string name)
        {
            if (Type.GetProperty(name, propertiesFlags) is PropertyInfo property)
                return property.GetCustomAttribute<NotMappedAttribute>() == null ? new ColumnInfo(registry, property) : null;

            if (Type.GetField(name, propertiesFlags) is FieldInfo fild)
                return fild.GetCustomAttribute<NotMappedAttribute>() == null ? new ColumnInfo(registry, fild) : null;

            return null;
        }

        /// <summary>
        /// Gets the column information for the specified member.
        /// </summary>
        /// <param name="member">The member representing the column.</param>
        /// <returns>The column information.</returns>
        public ColumnInfo GetColumn(MemberInfo member)
        {
            string name = ColumnInfo.GetName(member);
            return this.Columns.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// Gets the cells representing the column values of the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="ignorePrimaryKey">True to ignore the primary key column, false otherwise.</param>
        /// <param name="useForeign">If true and there is no column named Foreign Key Attribute.Name then use the primary key defined in the primary key object, otherwise do nothing with the primary key.</param>
        /// <returns>An enumerable of cells.</returns>
        public IEnumerable<Cell> GetCells(object owner, bool ignorePrimaryKey = false)
        {
            var fkCol = this.Columns.FirstOrDefault(c => !string.IsNullOrEmpty(c.ForeignKey));
            bool fkLoaded = false;

            foreach (var c in this.Columns)
            {
                object value = c.Get(owner);
                if ((c.Key && (ignorePrimaryKey || TranslationUtils.IsInvalidPk(value))) || !string.IsNullOrEmpty(c.ForeignKey))
                    continue;

                yield return new Cell(c.Name, value);
            }
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            if (fkColumn == null || !TranslationUtils.IsInvalidPk(value) || !(fkColumn.GetRaw(owner) is object fkInstance))
                return value;

            var fkPkColumn = TableTranslatorBase.GetTable(fkColumn.Type).Columns.FirstOrDefault(c => c.Key);
            return fkPkColumn.Get(fkInstance);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Type);
        }
    }
}
