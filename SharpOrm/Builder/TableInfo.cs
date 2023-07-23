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
        /// <param name="type">The type representing the table.</param>
        public TableInfo(Type type) : this(new TranslationRegistry(), type)
        {
        }

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
        }

        /// <summary>
        /// Gets the cells representing the column values of the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="ignorePrimaryKey">True to ignore the primary key column, false otherwise.</param>
        /// <param name="useForeign">If true and there is no column named Foreign Key Attribute.Name then use the primary key defined in the primary key object, otherwise do nothing with the primary key.</param>
        /// <returns>An enumerable of cells.</returns>
        public IEnumerable<Cell> GetCells(object owner, bool ignorePrimaryKey = false, bool readForeignKey = false)
        {
            var fkCol = this.Columns.FirstOrDefault(c => !string.IsNullOrEmpty(c.ForeignKey));
            bool hasPriorityFk = this.Columns.Any(c => c.Name == fkCol?.ForeignKey);

            foreach (var column in this.Columns)
            {
                bool isFkColumn = column == fkCol;
                if (isFkColumn && hasPriorityFk)
                    continue;

                object value = isFkColumn ? this.GetFkValue(owner, column.GetRaw(owner), column) : ProcessValue(column, owner, readForeignKey);
                if ((column.Key && (ignorePrimaryKey || TranslationUtils.IsInvalidPk(value))))
                    continue;

                yield return new Cell(isFkColumn ? column.ForeignKey : column.Name, value);
            }
        }

        private object ProcessValue(ColumnInfo column, object owner, bool readForeignKey)
        {
            object obj = column.Get(owner);
            if (!readForeignKey || !column.Type.IsClass || string.IsNullOrEmpty(column.ForeignKey) || TranslationUtils.IsNull(obj))
                return obj;

            var table = new TableInfo(column.Type);
            return table.Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var table = TableReaderBase.GetTable(fkColumn.Type);
            var pkColumn = table.Columns.First(c => c.Key);

            if (TranslationUtils.IsInvalidPk(value) || !(fkColumn.GetRaw(owner) is object fkInstance))
                return Activator.CreateInstance(pkColumn.Type);

            return pkColumn.Get(fkInstance);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Type);
        }
    }
}
