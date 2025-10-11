using SharpOrm.Builder.Tables;
using SharpOrm.Builder.Tables.Loaders;
using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a mapping between a type <typeparamref name="T"/> and a database table.
    /// </summary>
    /// <typeparam name="T">The type to be mapped to the table.</typeparam>
    public class ModelMapper<T> : ITableInfo, IModelMapper<T>, IModelMapper
    {
        private readonly ColumnLoader<T> _loader = null;
        private TableInfo table;

        internal SoftDeleteAttribute _softDelete;
        internal HasTimestampAttribute _timestamp;

        private string _name;

        /// <summary>
        /// Gets or sets the name of the table. 
        /// Throws an <see cref="InvalidOperationException"/> if the table has already been created.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (table != null)
                    throw new InvalidOperationException(Messages.Table.CannotChangeAfterBuild);

                _name = value;
            }
        }

        /// <summary>
        /// Gets the translation registry used for the mapping.
        /// </summary>
        public TranslationRegistry Registry { get; }

        Type ITableInfo.Type => typeof(T);

        SoftDeleteAttribute ITableInfo.SoftDelete => _softDelete;

        HasTimestampAttribute ITableInfo.Timestamp => _timestamp;

        IColumnLoader ITableInfo.ColumnLoader => _loader;

        public ModelMapper(QueryConfig config) : this(config.Translation)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMapper{T}"/> class.
        /// Automatically maps the properties and fields of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="registry">The translation registry to be used for the mapping.</param>
        public ModelMapper(TranslationRegistry registry)
        {
            Name = typeof(T).Name;
            Registry = registry;
            _loader = new ColumnLoader<T>(registry);
        }

        public ModelMapper(TranslationRegistry registry, NestedMode nestedMode)
        {
            Name = typeof(T).Name;
            Registry = registry;
            _loader = new ColumnLoader<T>(registry, nestedMode);
        }

        public IModelMapper<T> ApplyConfiguration(IModelMapperConfiguration<T> configuration)
        {
            configuration.Configure(this);
            return this;
        }

        public IModelMapper<T> SoftDelete(string column, string dateColumn = null)
        {
            _softDelete = new SoftDeleteAttribute(column) { DateColumnName = dateColumn };
            return this;
        }

        public IModelMapper<T> HasTimeStamps(string createdAtColumn, string updatedAtColumn)
        {
            _timestamp = new HasTimestampAttribute { CreatedAtColumn = createdAtColumn, UpdatedAtColumn = updatedAtColumn };
            return this;
        }

        /// <summary>
        /// Sets the key property for the table using the specified expression.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be used as the key.</param>
        /// <returns>The current <see cref="ModelMapper{T}"/> instance.</returns>
        public IModelMapper<T> HasKey(Expression<Func<T, object>> expression)
        {
            Property(expression).SetKey(true);
            return this;
        }

        /// <summary>
        /// Maps a property to a specific column name in the table.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be mapped.</param>
        /// <param name="columnName">The name of the column in the table.</param>
        /// <returns>A <see cref="ColumnMapInfo"/> representing the mapped column.</returns>
        public ColumnMapInfo Property(Expression<Func<T, object>> expression, string columnName)
        {
            return Property(expression).HasColumnName(columnName);
        }

        /// <summary>
        /// Maps a property using the specified expression.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be mapped.</param>
        /// <returns>A <see cref="ColumnMapInfo"/> representing the mapped property.</returns>
        public ColumnMapInfo Property(Expression<Func<T, object>> expression)
        {
            return _loader.GetColumnFromExpression(expression, true, out _)?.GetColumn(Registry) ?? throw new InvalidOperationException(Messages.Expressions.Invalid);
        }

        /// <summary>
        /// Maps a nested property with a specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to use for the nested property.</param>
        /// <returns></returns>
        public IModelMapper<T> MapNested(Expression<Func<T, object>> expression, string prefix = null, bool subNested = false)
        {
            var column = _loader.GetColumnFromExpression(expression, false, out _);
            column.MapChildren(column.Member as PropertyInfo, subNested ? NestedMode.All : NestedMode.Attribute);
            column._prefix = prefix;
            return this;
        }

        /// <summary>
        /// Builds the table mapping and registers it with the translation registry.
        /// </summary>
        /// <returns>A <see cref="TableInfo"/> representing the built table mapping.</returns>
        public TableInfo Build()
        {
            if (_loader.Nodes.Count == 0) return null;
            if (table != null) return table;

            return table = Registry.AddTableMap(this);
        }

        internal Column GetColumn(Expression<ColumnExpression<T>> columnExpression)
        {
            if (table == null)
                throw new Exception(Messages.Table.NotBuilded);

            var column = _loader.LoadColumns().Find(columnExpression);

            return column == null ? null : new Column(column.Name);
        }
    }
}
