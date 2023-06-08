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
        private Type type;

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

            this.type = type;
            this.Name = type.GetCustomAttribute<TableAttribute>(false)?.Name ?? type.Name;
            this.Column = this.GetColumns(config).ToArray();
        }

        private IEnumerable<ColumnInfo> GetColumns(TranslationRegistry config)
        {
            foreach (var prop in type.GetProperties(propertiesFlags))
                if (prop.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(config, prop);

            foreach (var field in type.GetFields(propertiesFlags))
                if (field.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(config, field);
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
        public IEnumerable<Cell> GetCells(object owner, bool ignorePrimaryKey = false)
        {
            foreach (var c in this.Column)
            {
                object value = c.Get(owner);
                if (c.Key && (ignorePrimaryKey || TranslationUtils.IsInvalidPk(value)))
                    continue;

                yield return new Cell(c.Name, value);
            }
        }
    }
}
