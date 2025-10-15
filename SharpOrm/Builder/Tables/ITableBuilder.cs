using SharpOrm.Builder.Grammars.Table.Constraints;
using System;
using System.Linq.Expressions;

namespace SharpOrm.Builder.Tables
{
    /// <summary>
    /// Provides a strongly-typed fluent API for building database table schemas with compile-time type safety.
    /// </summary>
    /// <typeparam name="T">The entity type that represents the table structure.</typeparam>
    public interface ITableBuilder<T> : ITableBuilder
    {
        /// <summary>
        /// Adds a column to the table using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties from the entity type.</param>
        /// <returns>An <see cref="IColumnBuilder"/> for further column configuration.</returns>
        IColumnBuilder Column(Expression<ColumnExpression<T>> expression);

        /// <summary>
        /// Adds an index on one or more columns using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to index.</param>
        /// <returns>An <see cref="IIndexBuilder"/> for further index configuration.</returns>
        IIndexBuilder HasIndex(Expression<ColumnExpression<T>> expression);

        /// <summary>
        /// Adds a primary key constraint using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to form the primary key.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        ITableBuilder<T> HasKey(Expression<ColumnExpression<T>> expression);

        /// <summary>
        /// Adds a unique constraint using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to form the unique constraint.</param>
        /// <param name="constraintName">Optional custom name for the constraint.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        ITableBuilder<T> HasUnique(Expression<ColumnExpression<T>> expression, string constraintName = null);

        /// <summary>
        /// Excludes specific columns from the table schema using a strongly-typed expression.
        /// </summary>
        /// <param name="expression">An expression that selects one or more properties to ignore.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        ITableBuilder<T> Ignore(Expression<ColumnExpression<T>> expression);

        /// <summary>
        /// Creates a table based on an existing table with optional column selection using expressions.
        /// </summary>
        /// <param name="table">The name of the source table.</param>
        /// <param name="columnExpression">Optional expression to select specific columns from the entity.</param>
        /// <param name="exceptColumns">When true, selects all columns except those in the expression; when false, selects only the columns in the expression.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        ITableBuilder<T> SetBasedTable(string table, Expression<ColumnExpression<T>> columnExpression, bool exceptColumns = false);

        /// <summary>
        /// Includes only the specified columns in the table schema, excluding all others.
        /// </summary>
        /// <param name="expression">An expression that selects the properties to include.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for method chaining.</returns>
        ITableBuilder<T> Only(Expression<ColumnExpression<T>> expression);
    }

    public interface ITableBuilder
    {
        IMetadata Metadata { get; }

        ITableBuilder SetName(string tableName);

        /// <summary>
        /// Adds a primary key constraint.
        /// </summary>
        /// <param name="columnNames">The column names that form the primary key.</param>
        /// <returns>The current ITableBuilder instance.</returns>
        ITableBuilder HasKey(params string[] columnNames);

        /// <summary>
        /// Adds a foreign key constraint.
        /// </summary>
        /// <param name="columnName">The foreign key column name.</param>
        /// <param name="referencedTable">The referenced table name.</param>
        /// <param name="referencedColumn">The referenced column name.</param>
        /// <param name="constraintName">The constraint name (optional).</param>
        /// <returns>The current ITableBuilder instance.</returns>
        ITableBuilder HasForeignKey(string columnName, string referencedTable, string referencedColumn = "Id", string constraintName = null);

        /// <summary>
        /// Adds a unique constraint for multiple columns.
        /// </summary>
        /// <param name="columnNames">The column names that form the unique constraint.</param>
        /// <param name="constraintName">The constraint name (optional).</param>
        /// <returns>The current ITableBuilder instance.</returns>
        ITableBuilder HasUnique(string[] columnNames, string constraintName = null);

        /// <summary>
        /// Adds a unique constraint for a single column.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="constraintName">The constraint name (optional).</param>
        /// <returns>The current ITableBuilder instance.</returns>
        ITableBuilder HasUnique(string columnName, string constraintName = null);

        /// <summary>
        /// Adds a check constraint.
        /// </summary>
        /// <param name="expression">The check expression.</param>
        /// <param name="constraintName">The constraint name (optional).</param>
        /// <returns>The current ITableBuilder instance.</returns>
        ITableBuilder HasCheck(string expression, string constraintName);

        ITableBuilder AddConstraint(Constraint constraint);

        ITableBuilder Ignore(string columnName);

        IIndexBuilder HasIndex(string columnName);

        IIndexBuilder HasIndex(params string[] columnNames);

        /// <summary>
        /// Adds a new column to the table schema.
        /// </summary>
        /// <param name="columnName">The name of the column to add.</param>
        /// <param name="type">The .NET type of the column.</param>
        /// <returns>An <see cref="IColumnBuilder"/> instance for further column configuration.</returns>
        IColumnBuilder AddColumn(string columnName, Type type);

        /// <summary>
        /// Adds a new column to the table schema using a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The .NET type of the column.</typeparam>
        /// <param name="columnName">The name of the column to add.</param>
        /// <returns>An <see cref="IColumnBuilder"/> instance for further column configuration.</returns>
        IColumnBuilder AddColumn<T>(string columnName);

        /// <summary>
        /// Builds and returns the table schema.
        /// Once built, the schema becomes immutable and the builder cannot be modified further.
        /// </summary>
        /// <returns>An <see cref="ITableSchema"/> instance representing the complete table definition.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the table name is null or empty.</exception>
        ITableSchema GetSchema();
    }
}
