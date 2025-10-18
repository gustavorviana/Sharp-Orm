using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder.Tables
{
    /// <summary>
    /// Generic table builder that provides strongly-typed table creation and configuration using expressions.
    /// </summary>
    /// <typeparam name="T">The entity type that represents the table structure.</typeparam>
    public class TableBuilder<T> : TableBuilder, ITableBuilder<T>
    {
        private readonly TableInfo _tableInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableBuilder{T}"/> class.
        /// </summary>
        /// <param name="registry">The translation registry containing entity metadata.</param>
        /// <param name="temporary">Indicates whether the table is temporary. If true, a GUID prefix will be added to the table name.</param>
        public TableBuilder(TranslationRegistry registry, bool temporary) : base(temporary)
        {
            _tableInfo = registry.GetTable<T>();
            SetName(_tableInfo.Name);

            foreach (var column in _tableInfo.Columns)
                AddColumn(column);
        }

        /// <summary>
        /// Creates a table based on an existing table with optional column selection using expressions.
        /// </summary>
        /// <param name="table">The name of the source table.</param>
        /// <param name="columnExpression">Optional expression to select specific columns from the entity.</param>
        /// <param name="exceptColumns">When true, selects all columns except those in the expression; when false, selects only the columns in the expression.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        public ITableBuilder<T> SetBasedTable(string table, Expression<ColumnExpression<T>> columnExpression, bool exceptColumns = false)
        {
            var query = Query<T>.ReadOnly(table);
            if (columnExpression != null)
            {
                var columns = GetColumnNames(columnExpression, exceptColumns);
                Metadata.Add(Metadatas.BasedColumns, columns);
                query.Where(new SqlExpression("1!=1"));
                query.Select(columns);
            }

            base.SetBasedQuery(query);
            return this;
        }

        /// <summary>
        /// Adds a column to the table using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties from the entity type.</param>
        /// <returns>An <see cref="IColumnBuilder"/> for further column configuration.</returns>
        public IColumnBuilder Column(Expression<ColumnExpression<T>> expression)
        {
            return base.AddColumn(_tableInfo.Columns.Find(expression));
        }

        /// <summary>
        /// Adds a primary key constraint using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to form the primary key.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        public ITableBuilder<T> HasKey(Expression<ColumnExpression<T>> expression)
        {
            base.HasKey(GetColumnNames(expression));
            return this;
        }

        /// <summary>
        /// Adds a unique constraint using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to form the unique constraint.</param>
        /// <param name="constraintName">Optional custom name for the constraint.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        public ITableBuilder<T> HasUnique(Expression<ColumnExpression<T>> expression, string constraintName = null)
        {
            base.HasUnique(GetColumnNames(expression), constraintName);
            return this;
        }

        /// <summary>
        /// Ignores specific columns in the table schema using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to ignore.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        public ITableBuilder<T> Ignore(Expression<ColumnExpression<T>> expression)
        {
            foreach (var name in GetColumnNames(expression))
                base.Ignore(name);

            return this;
        }

        /// <summary>
        /// Adds an index on one or more columns using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to index.</param>
        /// <returns>An <see cref="IIndexBuilder"/> for further index configuration.</returns>
        public IIndexBuilder HasIndex(Expression<ColumnExpression<T>> expression)
        {
            return base.HasIndex(GetColumnNames(expression));
        }

        /// <summary>
        /// Includes only the specified columns in the table schema, ignoring all others.
        /// </summary>
        /// <param name="expression">An expression that selects the properties to include.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        public ITableBuilder<T> Only(Expression<ColumnExpression<T>> expression)
        {
            var names = GetColumnNames(expression);
            foreach (var name in ColumnNames)
                if (name.Contains(name))
                    Ignore(name);

            return this;
        }

        /// <summary>
        /// Extracts column names from a strongly-typed expression.
        /// </summary>
        /// <param name="expression">The expression containing property selections.</param>
        /// <param name="except">When true, returns all columns except those in the expression; when false, returns only the columns in the expression.</param>
        /// <returns>An array of column names.</returns>
        private string[] GetColumnNames(Expression<ColumnExpression<T>> expression, bool except = false)
        {
            var columns = _tableInfo.Columns.FindAll(expression).Select(x => x.Name).ToArray();
            if (!except)
                return columns;

            return _tableInfo.Columns.Where(x => !columns.Contains(x.Name)).Select(x => x.Name).ToArray();
        }
    }

    /// <summary>
    /// Provides fluent API for building database table schemas with columns, constraints, and indexes.
    /// </summary>
    public class TableBuilder : ITableBuilder
    {
        private string _name;
        private TableSchema _schema;
        private readonly Metadata _metadata = new Metadata();
        private readonly List<Constraint> _constraints = new List<Constraint>();
        private readonly HashSet<string> _ignoredColumns = new HashSet<string>();
        private readonly List<IndexDefinition> _indexes = new List<IndexDefinition>();
        private readonly Dictionary<string, ColumnBuilder> _columns = new Dictionary<string, ColumnBuilder>();

        /// <summary>
        /// Gets an array of all column names currently defined in the table.
        /// </summary>
        public string[] ColumnNames => _columns.Keys.ToArray();

        /// <summary>
        /// Gets a value indicating whether the schema has been built and is immutable.
        /// </summary>
        protected bool Builded => _schema != null;

        /// <summary>
        /// Gets the metadata associated with this table builder.
        /// </summary>
        public IMetadata Metadata => _metadata;

        /// <summary>
        /// Gets a value indicating whether this table is temporary.
        /// Temporary tables have a GUID prefix added to their name.
        /// </summary>
        public bool Temporary { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableBuilder"/> class.
        /// </summary>
        /// <param name="table">The name of the table.</param>
        /// <param name="temporary">Indicates whether the table is temporary. If true, a GUID prefix will be added to the table name.</param>
        public TableBuilder(string table, bool temporary)
        {
            Temporary = temporary;
            _name = table;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableBuilder"/> class.
        /// </summary>
        /// <param name="temporary">Indicates whether the table is temporary. If true, a GUID prefix will be added to the table name.</param>
        protected TableBuilder(bool temporary)
        {
            Temporary = temporary;
        }

        /// <summary>
        /// Sets the name of the table.
        /// </summary>
        /// <param name="tableName">The name to assign to the table.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        public ITableBuilder SetName(string tableName)
        {
            _name = tableName;
            return this;
        }

        /// <summary>
        /// Creates a table based on an existing table with optional column selection.
        /// </summary>
        /// <param name="table">The name of the source table.</param>
        /// <param name="columns">Optional array of column names to include from the source table.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        public ITableBuilder SetBasedTable(string table, params string[] columns)
        {
            var query = Query.ReadOnly(table);
            if (columns?.Length > 0)
            {
                query.Where(new SqlExpression("1!=1"));
                Metadata.Add(Metadatas.BasedColumns, columns);
                query.Select(columns);
            }

            return SetBasedQuery(query);
        }

        /// <summary>
        /// Creates a table based on a query result.
        /// </summary>
        /// <param name="query">The query whose result will define the table structure.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        public ITableBuilder SetBasedQuery(QueryBase query)
        {
            Metadata.Add(Metadatas.BasedQuery, query);
            _columns.Clear();
            return this;
        }

        /// <summary>
        /// Adds a check constraint to the table.
        /// </summary>
        /// <param name="expression">The SQL expression that defines the check condition.</param>
        /// <param name="constraintName">The name of the check constraint.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built.</exception>
        public ITableBuilder HasCheck(string expression, string constraintName)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(new CheckConstraint(_name, expression, constraintName));
            return this;
        }

        /// <summary>
        /// Adds a foreign key constraint to the table.
        /// </summary>
        /// <param name="columnName">The name of the foreign key column in this table.</param>
        /// <param name="referencedTable">The name of the referenced table.</param>
        /// <param name="referencedColumn">The name of the referenced column (defaults to "Id").</param>
        /// <param name="constraintName">Optional custom name for the constraint.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built.</exception>
        public ITableBuilder HasForeignKey(string columnName, string referencedTable, string referencedColumn = "Id", string constraintName = null)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(new ForeignKeyConstraint(_name, columnName, referencedTable, referencedColumn, constraintName));
            return this;
        }

        /// <summary>
        /// Adds a primary key constraint to the table.
        /// </summary>
        /// <param name="columnNames">The names of columns that form the primary key.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built or if a specified column doesn't exist.</exception>
        public ITableBuilder HasKey(params string[] columnNames)
        {
            ThrowAlreadyBuilded();

            foreach (var columnName in columnNames)
            {
                if (!_columns.TryGetValue(columnName, out var column))
                    throw new InvalidOperationException($"The column '{columnName}' does not exist and cannot be set as a primary key.");

                column.IsRequired();
            }

            _constraints.Add(new PrimaryKeyConstraint(_name, columnNames));
            return this;
        }

        /// <summary>
        /// Adds a unique constraint to a single column.
        /// </summary>
        /// <param name="columnName">The name of the column to make unique.</param>
        /// <param name="constraintName">Optional custom name for the constraint.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        public ITableBuilder HasUnique(string columnName, string constraintName = null)
        {
            return HasUnique(new string[] { columnName }, constraintName);
        }

        /// <summary>
        /// Adds a unique constraint to multiple columns.
        /// </summary>
        /// <param name="columnNames">The names of columns that form the unique constraint.</param>
        /// <param name="constraintName">Optional custom name for the constraint.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built.</exception>
        public ITableBuilder HasUnique(string[] columnNames, string constraintName = null)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(new UniqueConstraint(_name, columnNames, constraintName));
            return this;
        }

        /// <summary>
        /// Adds a custom constraint to the table.
        /// </summary>
        /// <param name="constraint">The constraint to add.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built.</exception>
        public ITableBuilder AddConstraint(Constraint constraint)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(constraint);
            return this;
        }

        /// <summary>
        /// Adds a column to the table using column metadata information.
        /// </summary>
        /// <param name="columnInfo">The column metadata containing name, type, and other properties.</param>
        /// <returns>An <see cref="IColumnBuilder"/> for further column configuration.</returns>
        public IColumnBuilder AddColumn(ColumnInfo columnInfo)
        {
            var column = AddColumn(columnInfo.Name, columnInfo.Type);

            if (!string.IsNullOrEmpty(columnInfo.TypeName))
                column.HasColumnType(columnInfo.TypeName);

            if (columnInfo.Translation is INullableSqlTranslation)
                column.IsOptional();

            return column;
        }

        /// <summary>
        /// Adds a new column to the table.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="type">The .NET type of the column.</param>
        /// <returns>An <see cref="IColumnBuilder"/> for further column configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built, a base query has been defined, or the column has been ignored.</exception>
        public IColumnBuilder AddColumn(string columnName, Type type)
        {
            ThrowAlreadyBuilded();

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (Metadata.HasKey(Metadatas.BasedQuery))
                throw new InvalidOperationException("Cannot add a column when a base query has been defined for this table.");

            if (_ignoredColumns.Contains(columnName))
                throw new InvalidOperationException($"The column '{columnName}' has been ignored and cannot be added.");

            if (_columns.TryGetValue(columnName, out ColumnBuilder builder))
                return builder.HasType(type);

            type = TranslationUtils.GetValidTypeFor(type, out bool isNullable);
            var column = new ColumnBuilder(this, new System.Data.DataColumn(columnName, type));

            if (isNullable)
                column.IsOptional();

            _columns.Add(columnName, column);
            return column;
        }

        /// <summary>
        /// Adds a new column to the table using a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The .NET type of the column.</typeparam>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>An <see cref="IColumnBuilder"/> for further column configuration.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built, a base query has been defined, or the column has been ignored.</exception>
        public IColumnBuilder AddColumn<T>(string columnName)
        {
            return AddColumn(columnName, typeof(T));
        }

        /// <summary>
        /// Excludes a column from the table schema.
        /// </summary>
        /// <param name="columnName">The name of the column to ignore.</param>
        /// <returns>The current <see cref="ITableBuilder"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built.</exception>
        public ITableBuilder Ignore(string columnName)
        {
            ThrowAlreadyBuilded();

            if (!_ignoredColumns.Add(columnName))
                return this;

            _columns.Remove(columnName);
            return this;
        }

        /// <summary>
        /// Adds an index on a single column.
        /// </summary>
        /// <param name="columnName">The name of the column to index.</param>
        /// <returns>An <see cref="IIndexBuilder"/> for further index configuration.</returns>
        public IIndexBuilder HasIndex(string columnName)
        {
            return HasIndex(new string[] { columnName });
        }

        /// <summary>
        /// Adds a composite index on multiple columns.
        /// </summary>
        /// <param name="columnNames">The names of columns to include in the index.</param>
        /// <returns>An <see cref="IIndexBuilder"/> for further index configuration.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built.</exception>
        public IIndexBuilder HasIndex(params string[] columnNames)
        {
            ThrowAlreadyBuilded();

            var builder = new IndexBuilder(columnNames);
            _indexes.Add(builder.Definition);
            return builder;
        }

        /// <summary>
        /// Throws an exception if the schema has already been built.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the schema has already been built.</exception>
        private void ThrowAlreadyBuilded()
        {
            if (_schema != null)
                throw new InvalidOperationException("The table schema has already been built and cannot be modified.");
        }

        /// <summary>
        /// Builds and returns the table schema.
        /// Once built, the schema becomes immutable and the builder cannot be modified further.
        /// </summary>
        /// <returns>An <see cref="ITableSchema"/> instance representing the complete table definition.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the table name is null or empty.</exception>
        public virtual ITableSchema GetSchema()
        {
            if (_schema != null)
                return _schema;

            if (string.IsNullOrEmpty(_name))
                throw new InvalidOperationException("The table name cannot be null or empty. Use SetName to define a table name.");

            var tableName = Temporary ? Guid.NewGuid().ToString("N") + "_" + _name : _name;

            if (Temporary)
                foreach (var constraint in _constraints)
                    constraint.Table = tableName;

            return _schema = new TableSchema(tableName,
                Temporary,
                _constraints.AsReadOnly(),
                _indexes.AsReadOnly(),
                _columns.Select(x => x.Value._column).ToArray(),
                _metadata.MakeReadonly()
            );
        }

        /// <summary>
        /// Represents an immutable table schema definition containing columns, constraints, indexes, and metadata.
        /// </summary>
        private class TableSchema : ITableSchema
        {
            /// <summary>
            /// Gets the list of constraints defined for this table.
            /// </summary>
            public IReadOnlyList<Constraint> Constraints { get; }

            /// <summary>
            /// Gets the list of indexes defined for this table.
            /// </summary>
            public IReadOnlyList<IndexDefinition> Indexes { get; }

            /// <summary>
            /// Gets the list of columns defined for this table.
            /// </summary>
            public IReadOnlyList<System.Data.DataColumn> Columns { get; }

            /// <summary>
            /// Gets the name of the table.
            /// For temporary tables, this includes the GUID prefix.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets a value indicating whether this table is temporary.
            /// </summary>
            public bool Temporary { get; }

            /// <summary>
            /// Gets the metadata associated with this table schema.
            /// </summary>
            public IMetadata Metadata { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="TableSchema"/> class.
            /// </summary>
            /// <param name="name">The table name (including GUID prefix for temporary tables).</param>
            /// <param name="temporary">Indicates whether the table is temporary.</param>
            /// <param name="constraints">The collection of constraints for the table.</param>
            /// <param name="indexes">The collection of indexes for the table.</param>
            /// <param name="columns">The collection of columns for the table.</param>
            /// <param name="metadata">The metadata associated with the table.</param>
            public TableSchema(string name,
                bool temporary,
                IReadOnlyList<Constraint> constraints,
                IReadOnlyList<IndexDefinition> indexes,
                IReadOnlyList<System.Data.DataColumn> columns,
                IMetadata metadata)
            {
                Name = name;
                Temporary = temporary;
                Constraints = constraints;
                Indexes = indexes;
                Columns = columns;
                Metadata = metadata;
            }

            /// <summary>
            /// Creates a shallow copy of the current table schema.
            /// </summary>
            /// <returns>A new <see cref="ITableSchema"/> instance with the same properties.</returns>
            public ITableSchema Clone() => new TableSchema(Name, Temporary, Constraints, Indexes, Columns, Metadata);

            /// <summary>
            /// Creates a shallow copy of the current table schema.
            /// </summary>
            /// <returns>A new object instance with the same properties.</returns>
            object ICloneable.Clone() => Clone();
        }
    }
}
