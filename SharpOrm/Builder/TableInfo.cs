using SharpOrm.Builder.Expressions;
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
        private readonly ConcurrentDictionary<Type, TableInfo> cachedTables = new ConcurrentDictionary<Type, TableInfo>();
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

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation configuration and type.
        /// </summary>
        /// <param name="type">The type representing the table.</param>s
        public TableInfo(Type type) : this(type, TranslationRegistry.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation configuration and type.
        /// </summary>
        /// <param name="config">The translation configuration.</param>
        /// <param name="type">The type representing the table.</param>
        public TableInfo(Type type, TranslationRegistry config)
        {
            if (type == null || type.IsAbstract || type == typeof(Row))
                throw new InvalidOperationException($"Invalid type provided for the {nameof(TableInfo)} class.");

            this.Type = type;
            this.registry = config;
            this.Name = GetNameOf(type);
            this.Columns = GetColumns(Type, registry).ToArray();
        }

        public ColumnInfo[] GetPrimaryKeys()
        {
            return this.Columns.Where(c => c.Key).OrderBy(c => c.Order).ToArray();
        }

        public static IEnumerable<ColumnInfo> GetColumns<T>(Type type, TranslationRegistry registry, Expression<ColumnExpression<T>>[] calls, bool except)
        {
            var props = PropertyExpressionVisitor.VisitProperties(calls).ToArray();
            var columns = TableInfo.GetColumns(typeof(T), registry);

            if (except) columns.Where(x => !props.Contains(x.PropName));
            return columns.Where(x => props.Contains(x.PropName));
        }

        public static IEnumerable<ColumnInfo> GetColumns(Type type, TranslationRegistry registry)
        {
            foreach (var prop in type.GetProperties(propertiesFlags).Where(ColumnInfo.CanWork))
                yield return new ColumnInfo(registry, prop);

            foreach (var field in type.GetFields(propertiesFlags).Where(ColumnInfo.CanWork))
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
                if (columns.Any(x => x.Equals(column.Name, StringComparison.CurrentCultureIgnoreCase)))
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
            if (!(this.Columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)) is ColumnInfo col))
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
            foreach (var column in this.Columns)
            {
                if (!(properties is null) && properties.Any(x => x.Equals(column.PropName, StringComparison.CurrentCultureIgnoreCase)) != needContains)
                    continue;

                if (column.IsForeignKey)
                {
                    if (readFk && CanLoadForeignColumn(column))
                        yield return new Cell(column.ForeignKey, this.GetFkValue(owner, column.GetRaw(owner), column));
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
            return !this.Columns.Any(c => c != column && c.Name.Equals(column.ForeignKey, StringComparison.CurrentCultureIgnoreCase));
        }

        private object ProcessValue(ColumnInfo column, object owner, bool readForeignKey)
        {
            object obj = column.Get(owner);
            if (!readForeignKey || !column.Type.IsClass || !column.IsForeignKey || TranslationUtils.IsNull(obj))
                return obj;

            if (obj is null)
                return null;

            return new TableInfo(column.Type, this.registry).Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var table = this.Get(GetValidType(fkColumn.Type));
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

        /// <summary>
        /// Retrieve the object's table by checking the cache list.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The table info for the specified type.</returns>
        private TableInfo Get(Type type)
        {
            if (type == typeof(Row))
                return null;

            return cachedTables.GetOrAdd(type, _type => new TableInfo(_type));
        }
    }
}
