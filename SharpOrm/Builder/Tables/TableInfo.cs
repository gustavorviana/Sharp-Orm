using DbRunTest.Comparators;
using SharpOrm.Builder.Tables;
using SharpOrm.Builder.Tables.Loaders;
using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
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
        private readonly object _readLock = new object();
        internal readonly TranslationRegistry _registry;
        private ObjectReaderBase _reader = null;

        public Type Type { get; }
        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Gets an array of column information for the table.
        /// </summary>
        public ColumnCollection Columns { get; }

        public SoftDeleteAttribute SoftDelete { get; }

        public HasTimestampAttribute Timestamp { get; }

        internal TableInfo(ITableInfo info)
        {
            Timestamp = info.Timestamp;
            _registry = info.Registry;
            Type = info.Type;
            Name = info.Name;

            Columns = info.ColumnLoader.LoadColumns();
            SoftDelete = info.SoftDelete;
        }

        internal TableInfo(Type type, TranslationRegistry registry)
        {
            if (type == null || type.IsAbstract || type == typeof(Row))
                throw new InvalidOperationException(Messages.Table.InvalidType);

            Type = type;
            _registry = registry;
            Name = GetNameOf(type);
            Columns = new ColumnLoader(type, registry).LoadColumns();
            SoftDelete = type.GetCustomAttribute<SoftDeleteAttribute>();
            Timestamp = type.GetCustomAttribute<HasTimestampAttribute>();
        }

        public ColumnInfo[] GetPrimaryKeys()
        {
            return Columns.Where(c => c.Key).OrderBy(c => c.Order).ToArray();
        }

        public IEnumerable<ColumnInfo> GetColumns<T>(Expression<ColumnExpression<T>>[] calls, bool except)
        {
            var props = calls.Select(ExpressionUtils<T>.GetPropName).ToArray();

            if (except) Columns.Where(x => !props.ContainsIgnoreCase(x.PropName));
            return Columns.Where(x => props.ContainsIgnoreCase(x.PropName));
        }

        /// <summary>
        /// Validate fields of the object.
        /// </summary>
        /// <param name="owner">Object to be validated.</param>
        /// <param name="columns">Fields/Property of the object to be validated.</param>
        public void Validate(object owner, params string[] columns)
        {
            if (columns.Length == 0)
            {
                Validate(owner);
                return;
            }

            var context = new ValidationContext(owner);

            foreach (var column in Columns)
                if (columns.ContainsIgnoreCase(column.Name))
                    column.Validate(context);
        }

        /// <summary>
        /// Validate fields of the object.
        /// </summary>
        /// <param name="owner"></param>
        public void Validate(object owner)
        {
            var context = new ValidationContext(owner);

            foreach (var item in Columns) item.Validate(context);
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

            lock (_readLock)
                return this.GetConfiguredReader(readPk, readFk, null, false, validate).ReadRow(owner);
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
                throw new KeyNotFoundException(string.Format(Messages.Table.KeyNotFound, name, Type.FullName));

            return col.Get(owner);
        }

        internal object GetValue(object owner, MemberInfo column)
        {
            if (!(this.Columns.FirstOrDefault(c => c._column == column) is ColumnInfo col))
                throw new KeyNotFoundException(string.Format(Messages.Table.KeyNotFound, column.Name, this.Type.FullName));

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
            lock (_readLock)
                return this.GetConfiguredReader(readPk, readFk, properties, needContains, validate).ReadCells(owner);
        }

        private ObjectReaderBase GetConfiguredReader(bool readPk, bool readFk, string[] properties, bool needContains, bool validate)
        {
            if (_reader == null)
                _reader = ObjectReader.Create(this);

            _reader.ReadPk = readPk;
            _reader.ReadFk = readFk;

            if (needContains) _reader.Only(properties);
            else _reader.Except(properties);

            _reader.Validate = validate;

            return _reader;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Type);
        }

        internal static string GetNameOf(Type type)
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
            return Columns.Find(name);
        }

        internal ColumnInfo GetColumn(MemberInfo member)
        {
            return Columns.FirstOrDefault(c => MemberInfoComparator.Default.Equals(c._column, member));
        }
    }
}
