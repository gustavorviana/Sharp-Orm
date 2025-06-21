using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static SharpOrm.Msg.Messages;

namespace SharpOrm.DataTranslation
{
    public abstract class ObjectReaderBase
    {
        protected readonly TableInfo _table;
        public TranslationRegistry Translation { get; set; } = TranslationRegistry.Default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ReadMode _primaryKeyMode = ReadMode.None;
        /// <summary>
        /// Gets or sets the mode for reading primary keys.
        /// </summary>
        public ReadMode PrimaryKeyMode
        {
            get => _primaryKeyMode;
            set
            {
                _primaryKeyMode = value;
                OnCriterioChange();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to read primary keys.
        /// </summary>
        public bool ReadPk
        {
            get => PrimaryKeyMode != ReadMode.None;
            set => PrimaryKeyMode = value ? ReadMode.ValidOnly : ReadMode.None;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _readFk = true;

        /// <summary>
        /// Gets or sets a value indicating whether to read foreign keys.
        /// </summary>
        public bool ReadFk
        {
            get => _readFk;
            set
            {
                if (value == _readFk)
                    return;

                _readFk = value;
                OnCriterioChange();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to validate the object.
        /// </summary>
        public bool Validate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore timestamp columns.
        /// </summary>
        public bool IgnoreTimestamps { get; set; }

        private bool _isCreate = false;
        /// <summary>
        /// Gets or sets a value indicating whether the object is being created.
        /// </summary>
        public bool IsCreate
        {
            get => _isCreate;
            set
            {
                _isCreate = value;
                OnCriterioChange();
            }
        }

        internal ObjectReaderBase(TableInfo table)
        {
            _table = table;
        }

        public static ObjectReaderBase Create<T>(TranslationRegistry registry)
        {
            return Create(registry.GetTable(typeof(T)));
        }

        public static ObjectReaderBase Create(TableInfo table)
        {
            if (ReflectionUtils.IsDynamic(table.Type))
                return new DynamicObjectReader(table);

            return new ObjectReader(table);
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to only read the specified columns.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression specifying the columns to read.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReaderBase Only<K>(Expression<ColumnExpression<K>> expression)
        {
            SetExpression(expression, true);
            return this;
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to only read the specified columns.
        /// </summary>
        /// <param name="columns">The columns to read.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReaderBase Only(params string[] columns)
        {
            SetColumns(columns, true);
            return this;
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to exclude the specified columns.
        /// </summary>
        /// <param name="columns">The columns to exclude.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReaderBase Except(params string[] columns)
        {
            SetColumns(columns, false);
            return this;
        }

        /// <summary>
        /// Configures the <see cref="ObjectReader"/> to exclude the specified columns.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression specifying the columns to exclude.</param>
        /// <returns>The configured <see cref="ObjectReader"/>.</returns>
        public ObjectReaderBase Except<K>(Expression<ColumnExpression<K>> expression)
        {
            SetExpression(expression, false);
            return this;
        }

        protected abstract void SetExpression<K>(Expression<ColumnExpression<K>> expression, bool needContains);

        protected abstract void SetColumns(string[] columns, bool needContains);

        protected IEnumerable<SqlMember> GetMembers<K>(Expression<ColumnExpression<K>> expression)
        {
            return new ExpressionProcessor<K>(null, Translation, ExpressionConfig.New, null).ParseExpression(expression);
        }

        protected IEnumerable<ColumnInfo> GetValidColumns()
        {
            return _table.Columns.Where(CanReadColumn);
        }

        private bool CanReadColumn(ColumnInfo column)
        {
            if (column.Key && !ReadPk)
                return false;

            return (column.ForeignInfo != null && CanReadFk(column)) || column.Translation != null;
        }

        private bool CanReadFk(ColumnInfo column)
        {
            return ReadFk && !_table.Columns.Any(c => c != column && c.Name.Equals(column.ForeignInfo?.ForeignKey, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>  
        /// Reads rows from a collection of objects and converts them into an array of <see cref="Row"/> instances.  
        /// </summary>  
        /// <param name="owners">The collection of objects to read.</param>  
        /// <returns>An array of <see cref="Row"/> instances representing the objects.</returns>  
        public Row[] ReadRows(params object[] owners)
        {
            return owners.Select(ReadRow).ToArray();
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

        /// <summary>
        /// Reads the cells from the specified object.
        /// </summary>
        /// <param name="owner">The object to read.</param>
        /// <returns>An enumerable of cells representing the object.</returns>
        public abstract IEnumerable<Cell> ReadCells(object owner);

        /// <summary>  
        /// Gets the names of the columns that are allowed to be read.  
        /// </summary>  
        /// <returns>An array of column names.</returns>  
        public abstract string[] GetColumnNames();

        protected bool CanUseKeyValue(object value)
        {
            if (PrimaryKeyMode == ReadMode.None)
                return false;

            return !TranslationUtils.IsInvalidPk(value) || PrimaryKeyMode == ReadMode.All;
        }

        protected TableInfo GetTable(Type type)
        {
            return _table.registry.GetTable(type);
        }

        /// <summary>
        /// Determines whether the specified object has a valid key.
        /// </summary>
        /// <param name="owner">The object to check.</param>
        /// <returns>True if the object has a valid key; otherwise, false.</returns>
        public bool HasValidKey(object owner)
        {
            return _table.Columns.Any(column => column.Key && !TranslationUtils.IsInvalidPk(ProcessValue(column, owner)));
        }

        protected object ProcessValue(ColumnInfo column, object owner)
        {
            object obj = column.Get(owner);
            if (!ReadFk || !column.Type.IsClass || column.ForeignInfo == null || TranslationUtils.IsNull(obj))
                return obj;

            return GetTable(column.Type).Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        protected virtual void OnCriterioChange()
        {

        }
    }
}
