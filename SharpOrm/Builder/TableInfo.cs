using SharpOrm.DataTranslation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents information about a database table.
    /// </summary>
    public class TableInfo
    {
        private static readonly BindingFlags propertiesFlags = BindingFlags.Instance | BindingFlags.Public;
        private readonly TranslationRegistry registry;

        public Type Type { get; }
        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets an array of column information for the table.
        /// </summary>
        public ColumnInfo[] Columns { get; }

        public string SoftDeleteColumn { get; }

        public bool CanSoftDelete => !(this.SoftDeleteColumn is null);
        
        internal bool IsManualMap { get; }

        internal TableInfo(Type type, TranslationRegistry registry, string name, IEnumerable<ColumnTreeInfo> columns)
        {
            this.IsManualMap = true;

            this.registry = registry;
            this.Type = type;
            this.Name = name;

            this.Columns = columns.ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation configuration and type.
        /// </summary>
        /// <param name="type">The type representing the table.</param>s
        [Obsolete("Use \"SharpOrm.DataTranslation.TranslationRegistry.Default.GetTable(Type)\". This constructor will be removed in version 3.x.")]
        public TableInfo(Type type) : this(type, TranslationRegistry.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation registry and type.
        /// </summary>
        /// <param name="registry">The translation registry.</param>
        /// <param name="type">The type representing the table.</param>
        [Obsolete("Use \"SharpOrm.DataTranslation.TranslationRegistry.Default.GetTable(Type)\". This constructor will be removed in version 3.x.")]
        public TableInfo(Type type, TranslationRegistry registry)
        {
            if (type == null || type.IsAbstract || type == typeof(Row))
                throw new InvalidOperationException($"Invalid type provided for the {nameof(TableInfo)} class.");

            this.Type = type;
            this.registry = registry;
            this.Name = GetNameOf(type);
            this.Columns = this.GetColumns().ToArray();
            this.SoftDeleteColumn = type.GetCustomAttribute<SoftDeleteAttribute>()?.ColumnName;
        }

        public ColumnInfo[] GetPrimaryKeys()
        {
            return this.Columns.Where(c => c.Key).OrderBy(c => c.Order).ToArray();
        }

        public IEnumerable<ColumnInfo> GetColumns<T>(Expression<ColumnExpression<T>>[] calls, bool except)
        {
            var props = calls.Select(ExpressionUtils<T>.GetPropName).ToArray();

            if (except) this.Columns.Where(x => !props.Contains(x.PropName));
            return this.Columns.Where(x => props.Contains(x.PropName));
        }

        private IEnumerable<ColumnInfo> GetColumns()
        {
            foreach (var prop in this.Type.GetProperties(propertiesFlags).Where(ColumnInfo.CanWork))
                yield return new ColumnInfo(registry, prop);

            foreach (var field in this.Type.GetFields(propertiesFlags).Where(ColumnInfo.CanWork))
                yield return new ColumnInfo(registry, field);
        }

        /// <summary>
        /// Validate fields of the object.
        /// </summary>
        /// <param name="owner">Object to be validated.</param>
        /// <param name="columns">Fields/Properties of the object to be validated.</param>
        public void Validate(object owner, params string[] columns)
        {
            if (columns.Length == 0)
            {
                this.Validate(owner);
                return;
            }

            foreach (var column in this.Columns)
                if (columns.ContainsIgnoreCase(column.Name))
                    column.Validate(owner);
        }

        /// <summary>
        /// Validate fields of the object.
        /// </summary>
        /// <param name="owner"></param>
        public void Validate(object owner)
        {
            foreach (var item in this.Columns) item.Validate(owner);
        }

        /// <summary>
        /// Transforms the object into a Row.
        /// </summary>
        /// <param name="owner">Object that should have its columns read.</param>
        /// <param name="readPk">Indicates whether primary keys can be retrieved.</param>
        /// <param name="readFk">Indicates whether foreign keys can be retrieved.</param>
        /// <returns></returns>
        public Row GetRow(object owner, bool readPk, bool readFk, bool validate)
        {
            if (owner is Row row)
                return row;

            return new Row(this.GetObjCells(owner, readPk, readFk, validate: validate).ToArray());
        }

        /// <summary>
        /// Retrieve the value of a column.
        /// </summary>
        /// <param name="owner">Object that contains the column.</param>
        /// <param name="name">Name of the column from which to retrieve the value.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public object GetValue(object owner, string name)
        {
            if (!(this.Columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is ColumnInfo col))
                throw new KeyNotFoundException($"The key '{name}' does not exist in the object '{this.Type.FullName}'.");

            return col.Get(owner);
        }

        /// <summary>
        /// Gets the cells representing the column values of the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="readPk">True to read the primary key column, false otherwise.</param>
        /// <param name="readFk">If true and there is no column named Foreign Key Attribute. Name then use the primary key defined in the primary key object, otherwise do nothing with the primary key.</param>
        /// <param name="properties">Name of the properties that should or should not be returned.</param>
        /// <param name="needContains">If true, only the columns with names in <paramref name="properties"/> will be returned; if false, only the properties not in the list will be returned.</param>
        /// <returns>An enumerable of cells.</returns>
        public IEnumerable<Cell> GetObjCells(object owner, bool readPk, bool readFk, string[] properties = null, bool needContains = true, bool validate = false)
        {
            for (int i = 0; i < this.Columns.Length; i++)
            {
                var column = this.Columns[i];

                if (!(properties is null) && properties.Any(x => x.Equals(column.PropName, StringComparison.OrdinalIgnoreCase)) != needContains)
                    continue;

                if (column.ForeignInfo != null)
                {
                    if (readFk && CanLoadForeignColumn(column))
                        yield return new Cell(column.ForeignInfo.ForeignKey, this.GetFkValue(owner, column.GetRaw(owner), column));
                    continue;
                }

                object value = ProcessValue(column, owner, readFk);
                if ((column.Key && (!readPk || TranslationUtils.IsInvalidPk(value))))
                    continue;

                if (validate)
                    column.ValidateValue(value);

                yield return new Cell(column.Name, value);
            }
        }

        private bool CanLoadForeignColumn(ColumnInfo column)
        {
            return !this.Columns.Any(c => c != column && c.Name.Equals(column.ForeignInfo?.ForeignKey, StringComparison.OrdinalIgnoreCase));
        }

        private object ProcessValue(ColumnInfo column, object owner, bool readForeignKey)
        {
            object obj = column.Get(owner);
            if (!readForeignKey || !column.Type.IsClass || column.ForeignInfo == null || TranslationUtils.IsNull(obj))
                return obj;

            if (obj is null)
                return null;

            return registry.GetTable(column.Type).Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var type = GetValidType(fkColumn.Type);
            if (type == typeof(Row))
                return null;

            var table = this.registry.GetTable(type);
            var pkColumn = table.Columns.First(c => c.Key);

            if (TranslationUtils.IsInvalidPk(value) || !(fkColumn.GetRaw(owner) is object fkInstance))
                return null;

            return pkColumn.Get(fkInstance);
        }

        public object CreateInstance()
        {
            return Activator.CreateInstance(this.Type);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Type);
        }

        [Obsolete("Use \"SharpOrm.DataTranslation.TranslationRegistry.Default.GetTableName(Type)\". This constructor will be removed in version 3.x.")]
        public static string GetNameOf(Type type)
        {
            if (!(GetValidType(type).GetCustomAttribute<TableAttribute>(false) is TableAttribute table) || string.IsNullOrEmpty(table.Name))
                return type.Name;

            if (string.IsNullOrEmpty(table.Schema))
                return table.Name;

            return string.Concat(table.Schema, ".", table.Name);
        }

        private static Type GetValidType(Type type)
        {
            return ReflectionUtils.IsCollection(type) ? ReflectionUtils.GetGenericArg(type) : type;
        }

        public ColumnInfo GetColumn(string name)
        {
            return this.Columns.FirstOrDefault(c => c.Name == name);
        }

        internal ColumnInfo GetColumn(MemberInfo member)
        {
            return this.Columns.FirstOrDefault(c => c.column == member);
        }
    }
}
