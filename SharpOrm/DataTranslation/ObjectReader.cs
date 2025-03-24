using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Class responsible for reading objects and translating them into rows.
    /// </summary>
    public class ObjectReader
    {
        #region Fields
        private string[] columns = DotnetUtils.EmptyArray<string>();

        private bool needContains;

        private readonly TableInfo table;
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether to read primary keys.
        /// </summary>
        public bool ReadPk { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to read foreign keys.
        /// </summary>
        public bool ReadFk { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate the object.
        /// </summary>
        public bool Validate { get; set; }

        private readonly bool hasUpdateColumn;
        private readonly bool hasCreateColumn;

        /// <summary>
        /// Gets or sets a value indicating whether to ignore timestamp columns.
        /// </summary>
        public bool IgnoreTimestamps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the object is being created.
        /// </summary>
        public bool IsCreate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReader"/> class.
        /// </summary>
        /// <param name="table">The table information.</param>
        public ObjectReader(TableInfo table)
        {
            this.table = table;
            this.hasUpdateColumn = !string.IsNullOrEmpty(table.Timestamp?.UpdatedAtColumn);
            this.hasCreateColumn = !string.IsNullOrEmpty(table.Timestamp?.CreatedAtColumn);
        }

        /// <summary>
        /// Creates an <see cref="ObjectReader"/> for the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="registry">The translation registry.</param>
        /// <returns>An <see cref="ObjectReader"/> for the specified type.</returns>
        public static ObjectReader OfType<T>(TranslationRegistry registry)
        {
            return new ObjectReader(registry.GetTable(typeof(T)));
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to only read the specified columns.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression specifying the columns to read.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReader Only<T>(Expression<ColumnExpression<T>> expression)
        {
            this.needContains = true;
            return this.SetExpression(expression);
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to only read the specified columns.
        /// </summary>
        /// <param name="columns">The columns to read.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReader Only(params string[] columns)
        {
            this.needContains = true;
            return this.SetColumns(columns);
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to exclude the specified columns.
        /// </summary>
        /// <param name="columns">The columns to exclude.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReader Except(params string[] columns)
        {
            this.needContains = false;
            return this.SetColumns(columns);
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to exclude the specified columns.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression specifying the columns to exclude.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReader Except<T>(Expression<ColumnExpression<T>> expression)
        {
            this.needContains = false;
            return this.SetExpression(expression);
        }

        private ObjectReader SetExpression<T>(Expression<ColumnExpression<T>> expression)
        {
            return this.SetColumns(
                new ExpressionProcessor<T>(null, ExpressionConfig.New).ParseColumnNames(expression).ToArray()
            );
        }

        private ObjectReader SetColumns(string[] columns)
        {
            this.columns = columns ?? DotnetUtils.EmptyArray<string>();
            return this;
        }

        /// <summary>
        /// Reads a row from the specified object.
        /// </summary>
        /// <param name="owner">The object to read.</param>
        /// <returns>A <see cref="Row"/> representing the object.</returns>
        public Row ReadRow(object owner)
        {
            return new Row(ReadCells(owner).ToArray());
        }

        #region ExpandoObject
        private IEnumerable<Cell> ReadDictCells(IDictionary<string, object> owner)
        {
            return owner
                .Where(item => IsAllowedName(item.Key) || (this.IsKey(item.Key) && this.UsePk(item.Value)))
                .Select(item => new Cell(item.Key, item.Value));
        }

        private bool IsKey(string name)
        {
            return name.Equals("id", StringComparison.OrdinalIgnoreCase);
        }

        private bool UsePk(object value)
        {
            return ReadPk && !TranslationUtils.IsInvalidPk(value);
        }
        #endregion

        /// <summary>
        /// Determines whether the specified object has a valid key.
        /// </summary>
        /// <param name="owner">The object to check.</param>
        /// <returns>True if the object has a valid key; otherwise, false.</returns>
        public bool HasValidKey(object owner)
        {
            return table.Columns.Any(column => column.Key && !TranslationUtils.IsInvalidPk(ProcessValue(column, owner)));
        }

        #region ObjectReader

        /// <summary>  
        /// Gets the names of the columns that are allowed to be read.  
        /// </summary>  
        /// <returns>An array of column names.</returns>  
        public string[] GetColumnNames()
        {
            List<string> names = new List<string>();
            names.AddRange(table.Columns.Where(column => CanRead(column) && !IsTimeStamps(column.Name)).Select(x => x.Name));
            if (IgnoreTimestamps)
                return names.ToArray();

            if (hasCreateColumn && IsCreate)
                names.Add(table.Timestamp.CreatedAtColumn);

            if (hasUpdateColumn)
                names.Add(table.Timestamp.UpdatedAtColumn);

            return names.ToArray();
        }

        /// <summary>
        /// Reads the cells from the specified object.
        /// </summary>
        /// <param name="owner">The object to read.</param>
        /// <returns>An enumerable of cells representing the object.</returns>
        public IEnumerable<Cell> ReadCells(object owner)
        {
            if (owner is IDictionary<string, object> dict)
                return ReadDictCells(dict);

            return ReadObjectCells(new ValidationContext(owner));
        }

        private IEnumerable<Cell> ReadObjectCells(ValidationContext context)
        {
            for (int i = 0; i < table.Columns.Length; i++)
            {
                if (!CanRead(table.Columns[i]) || IsTimeStamps(table.Columns[i].Name))
                    continue;

                var cell = GetCell(context, table.Columns[i]);
                if (cell != null) yield return cell;
            }

            if (hasCreateColumn && IsCreate)
                yield return new Cell(table.Timestamp.CreatedAtColumn, DateTime.UtcNow);

            if (hasUpdateColumn)
                yield return new Cell(table.Timestamp.UpdatedAtColumn, DateTime.UtcNow);
        }

        private Cell GetCell(ValidationContext context, ColumnInfo column)
        {
            if (column.ForeignInfo != null)
                return new Cell(column.ForeignInfo.ForeignKey, GetFkValue(context.ObjectInstance, column.GetRaw(context.ObjectInstance), column));

            object value = ProcessValue(column, context.ObjectInstance);
            if (column.Key && TranslationUtils.IsInvalidPk(value))
                return null;

            if (Validate) column.ValidateValue(context, value);
            return new Cell(column.Name, value);
        }

        private bool CanRead(ColumnInfo column)
        {
            if (!IsAllowedName(column.Name) || (column.Key && !ReadPk))
                return false;

            return column.ForeignInfo == null || CanReadFk(column);
        }

        private object ProcessValue(ColumnInfo column, object owner)
        {
            object obj = column.Get(owner);
            if (!ReadFk || !column.Type.IsClass || column.ForeignInfo == null || TranslationUtils.IsNull(obj))
                return obj;

            return table.registry.GetTable(column.Type).Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var type = GetValidType(fkColumn.Type);
            if (type == typeof(Row))
                return null;

            var table = this.table.registry.GetTable(type);
            var pkColumn = table.Columns.First(c => c.Key);

            if (TranslationUtils.IsInvalidPk(value) || !(fkColumn.GetRaw(owner) is object fkInstance))
                return null;

            return pkColumn.Get(fkInstance);
        }

        private static Type GetValidType(Type type)
        {
            return ReflectionUtils.IsCollection(type) ? ReflectionUtils.GetGenericArg(type) : type;
        }
        #endregion

        protected bool IsAllowedName(string name)
        {
            return columns.Length == 0 ||
            columns.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)) == needContains;
        }

        public bool IsTimeStamps(string name)
        {
            return (hasUpdateColumn && name.Equals(table.Timestamp.UpdatedAtColumn, StringComparison.OrdinalIgnoreCase)) ||
                (hasCreateColumn && name.Equals(table.Timestamp.CreatedAtColumn, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanReadFk(ColumnInfo column)
        {
            return ReadFk && !table.Columns.Any(c => c != column && c.Name.Equals(column.ForeignInfo?.ForeignKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
