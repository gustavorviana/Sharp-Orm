using SharpOrm.Builder.Grammars.Table.Constraints;
using System;

namespace SharpOrm.Builder.Tables
{
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
        /// <returns>An <see cref="IColumnBuilder"/> instance for further column configuration.</returns>
        IColumnBuilder AddColumn(string columnName, Type type);

        ITableSchema GetSchema();
    }
}
