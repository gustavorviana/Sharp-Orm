using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.DataTranslation
{
    public abstract class ObjectReaderBase
    {
        protected readonly TableInfo _table;
        private ObjectReaderSettings _settings = new ObjectReaderSettings();

        public ObjectReaderSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings == value)
                    throw new ArgumentNullException(nameof(value));


                _settings.OnChange -= OnSettingsChange;
                _settings = value;
                value.OnChange += OnSettingsChange;
            }
        }

        [Obsolete("Use Settings.ReadDatabaseGenerated instead.")]
        public bool ReadDatabaseGenerated
        {
            get => Settings.ReadDatabaseGenerated;
            set => Settings.ReadDatabaseGenerated = value;
        }

        [Obsolete("Use Settings.Translation instead.")]
        public TranslationRegistry Translation
        {
            get => Settings.Translation;
            set => Settings.Translation = value;
        }

        /// <summary>
        /// Gets or sets the mode for reading primary keys.
        /// </summary>
        [Obsolete("Use Settings.PrimaryKeyMode instead.")]
        public ReadMode PrimaryKeyMode
        {
            get => Settings.PrimaryKeyMode;
            set => Settings.PrimaryKeyMode = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to read primary keys.
        /// </summary>
        [Obsolete("Use Settings.PrimaryKeyMode instead.")]
        public bool ReadPk
        {
            get => Settings.PrimaryKeyMode != ReadMode.None;
            set => Settings.PrimaryKeyMode = value ? ReadMode.ValidOnly : ReadMode.None;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to read foreign keys.
        /// </summary>
        [Obsolete("Use Settings.ReadForeignKeys instead.")]
        public bool ReadFk
        {
            get => Settings.ReadForeignKeys;
            set => Settings.ReadForeignKeys = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to validate the object.
        /// </summary>
        [Obsolete("Use Settings.Validate instead.")]
        public bool Validate
        {
            get => Settings.Validate;
            set => Settings.Validate = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore timestamp columns.
        /// </summary>
        [Obsolete("Use Settings.IgnoreTimestamps instead.")]
        public bool IgnoreTimestamps
        {
            get => Settings.IgnoreTimestamps;
            set => Settings.IgnoreTimestamps = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the object is being created.
        /// </summary>
        [Obsolete("Use Settings.IsCreate instead.")]
        public bool IsCreate
        {
            get => Settings.IsCreate;
            set => Settings.IsCreate = value;
        }

        internal ObjectReaderBase(TableInfo table)
        {
            _table = table;
            _settings.OnChange += OnSettingsChange;
        }

        [Obsolete("This method is obsolete and will be removed in version 4.0. Use IObjectReaderFactory.OfType() instead.")]
        public static ObjectReaderBase Create<T>(TranslationRegistry registry)
        {
            return Create(registry.GetTable(typeof(T)));
        }

        [Obsolete("This method is obsolete and will be removed in version 4.0. Use IObjectReaderFactory.OfType() instead.")]
        public static ObjectReaderBase Create(TableInfo table)
        {
            if (ReflectionUtils.IsDynamic(table.Type))
                return new DynamicObjectReader(table);

            return new ObjectReader(table);
        }

        private void OnSettingsChange(object sender, EventArgs e) => OnCriteriaChange();

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
            return new ExpressionProcessor<K>(null, Settings.Translation, ExpressionConfig.New | ExpressionConfig.SubMembers, null).ParseExpression(expression);
        }

        protected bool CanReadColumn(ColumnInfo column)
        {
            if (!Settings.ReadDatabaseGenerated && column.DatabaseGenerated)
                return false;

            if (column.Key && Settings.PrimaryKeyMode == ReadMode.None)
                return false;

            return (column.ForeignInfo != null && CanReadFk(column)) || column.Translation != null;
        }

        private bool CanReadFk(ColumnInfo column)
        {
            return Settings.ReadForeignKeys && !_table.Columns.Any(c => c != column && c.Name.Equals(column.ForeignInfo?.ForeignKey, StringComparison.OrdinalIgnoreCase));
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
            if (Settings.PrimaryKeyMode == ReadMode.None)
                return false;

            return !TranslationUtils.IsInvalidPk(value) || Settings.PrimaryKeyMode == ReadMode.All;
        }

        protected TableInfo GetTable(Type type)
        {
            return _table._registry.GetTable(type);
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
            object obj = GetValue(column, owner);
            if (Settings.ReadForeignKeys || !column.Type.IsClass || column.ForeignInfo == null || TranslationUtils.IsNull(obj))
                return obj;

            return GetValue(GetTable(column.Type).Columns.FirstOrDefault(c => c.Key), obj);
        }

        internal static object GetRawValue(ColumnInfo info, object owner)
        {
            if (info is ColumnTreeInfo treeInfo)
                return treeInfo.InternalGetRaw(owner);

            return info.GetRaw(owner);
        }

        internal static object GetValue(ColumnInfo info, object owner)
        {
            if (info is ColumnTreeInfo treeInfo)
                return treeInfo.InternalGet(owner);

            return info.Get(owner);
        }

        protected virtual void OnCriteriaChange()
        {

        }
    }
}
