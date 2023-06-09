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
        public ColumnInfo[] Column { get; }

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
            this.Column = this.GetColumns(config).Distinct().Where(c => c != null).ToArray();
        }

        private IEnumerable<ColumnInfo> GetColumns(TranslationRegistry registry)
        {
            foreach (var prop in Type.GetProperties(propertiesFlags))
                if (prop.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, prop);

            foreach (var field in Type.GetFields(propertiesFlags))
                if (field.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, field);

            yield return GetByName(registry, "Id");
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
            return this.Column.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// Gets the cells representing the column values of the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="ignorePrimaryKey">True to ignore the primary key column, false otherwise.</param>
        /// <returns>An enumerable of cells.</returns>
        public IEnumerable<Cell> GetCells(object owner, bool ignorePrimaryKey = false, bool ignoreForeign = true)
        {
            foreach (var c in this.Column)
            {
                object value = c.Get(owner);
                if ((c.Key && (ignorePrimaryKey || TranslationUtils.IsInvalidPk(value))) || (ignoreForeign && !string.IsNullOrEmpty(c.ForeignKey)))
                    continue;

                yield return new Cell(c.Name, value);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Type);
        }
    }
}
