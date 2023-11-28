using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<Type, TableInfo> cachedTables = new ConcurrentDictionary<Type, TableInfo>();
        private static readonly BindingFlags propertiesFlags = BindingFlags.Instance | BindingFlags.Public;
        public Type Type { get; }
        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Indicates whether there is any column with a non-native type (Stream is considered native in this context).
        /// </summary>
        public bool HasNonNative { get; private set; }
        /// <summary>
        /// Indicates whether there is any column representing a foreign object.
        /// </summary>
        [Obsolete("It will be removed in version 2.x.x.")]
        public bool HasFk { get; private set; }
        /// <summary>
        /// Gets an array of column information for the table.
        /// </summary>
        public ColumnInfo[] Columns { get; }

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation configuration and type.
        /// </summary>
        /// <param name="type">The type representing the table.</param>
        public TableInfo(Type type) : this(TranslationRegistry.Default, type)
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
            this.Name = GetNameOf(type);
            this.Columns = this.GetColumns(config).ToArray();
        }

        private IEnumerable<ColumnInfo> GetColumns(TranslationRegistry registry)
        {
            foreach (var col in GetColumns(Type, registry))
            {
                if (col.IsForeignKey)
                    this.HasFk = true;

                if (!col.IsNative && !col.IsForeignKey)
                    this.HasNonNative = true;

                yield return col;
            }
        }

        public static IEnumerable<ColumnInfo> GetColumns(Type type, TranslationRegistry registry)
        {
            foreach (var prop in type.GetProperties(propertiesFlags))
                if (prop.CanRead && prop.CanWrite && prop.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, prop);

            foreach (var field in type.GetFields(propertiesFlags))
                if (!field.IsInitOnly && field.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, field);
        }

        /// <summary>
        /// Gets the cells representing the column values of the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="ignorePrimaryKey">True to ignore the primary key column, false otherwise.</param>
        /// <param name="readForeignKey">If true and there is no column named Foreign Key Attribute.Name then use the primary key defined in the primary key object, otherwise do nothing with the primary key.</param>
        /// <returns>An enumerable of cells.</returns>
        [Obsolete("Use " + nameof(GetObjCells), true)]
        public IEnumerable<Cell> GetCells(object owner, bool ignorePrimaryKey = false, bool readForeignKey = false)
        {
            return this.GetObjCells(owner, !ignorePrimaryKey, readForeignKey);
        }

        /// <summary>
        /// Transforms the object into a Row.
        /// </summary>
        /// <param name="owner">Object that should have its columns read.</param>
        /// <param name="readPk">Indicates whether primary keys can be retrieved.</param>
        /// <param name="readFk">Indicates whether foreign keys can be retrieved.</param>
        /// <returns></returns>
        public Row GetRow(object owner, bool readPk, bool readFk)
        {
            if (owner is Row row)
                return row;

            return new Row(this.GetObjCells(owner, readPk, readFk).ToArray());
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
            name = name.ToLower();
            if (!(this.Columns.FirstOrDefault(c => c.Name.ToLower() == name) is ColumnInfo col))
                throw new KeyNotFoundException($"The key '{name}' does not exist in the object '{this.Type.FullName}'.");

            return col.Get(owner);
        }

        /// <summary>
        /// Gets the cells representing the column values of the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="readPk">True to read the primary key column, false otherwise.</param>
        /// <param name="readFk">If true and there is no column named Foreign Key Attribute.Name then use the primary key defined in the primary key object, otherwise do nothing with the primary key.</param>
        /// <returns>An enumerable of cells.</returns>
        public IEnumerable<Cell> GetObjCells(object owner, bool readPk, bool readFk)
        {
            foreach (var column in this.Columns)
            {
                string propName = column.PropName;
                if (column.IsForeignKey)
                {
                    if (readFk && CanLoadForeignColumn(column))
                        yield return new Cell(column.ForeignKey, this.GetFkValue(owner, column.GetRaw(owner), column), propName);
                    continue;
                }

                object value = ProcessValue(column, owner, readFk);
                if ((column.Key && (!readPk || TranslationUtils.IsInvalidPk(value))))
                    continue;

                yield return new Cell(column.Name, value, propName);
            }
        }

        private bool CanLoadForeignColumn(ColumnInfo column)
        {
            return !this.Columns.Any(c => c != column && c.Name.Equals(column.ForeignKey, StringComparison.OrdinalIgnoreCase));
        }

        private object ProcessValue(ColumnInfo column, object owner, bool readForeignKey)
        {
            object obj = column.Get(owner);
            if (!readForeignKey || !column.Type.IsClass || !column.IsForeignKey || TranslationUtils.IsNull(obj))
                return obj;

            if (obj is null)
                return null;

            return new TableInfo(column.Type).Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var table = TableInfo.Get(GetValidType(fkColumn.Type));
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
            return GetValidType(type).GetCustomAttribute<TableAttribute>(false)?.Name ?? type.Name;
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
        public static TableInfo Get(Type type)
        {
            if (type == typeof(Row))
                return null;

            return cachedTables.GetOrAdd(type, _type => new TableInfo(_type));
        }
    }
}
