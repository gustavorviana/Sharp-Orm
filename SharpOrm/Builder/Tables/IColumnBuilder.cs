using System;

namespace SharpOrm.Builder.Tables
{
    /// <summary>
    /// Defines a contract for configuring individual columns with ExtendedPropertyKeys support.
    /// </summary>
    public interface IColumnBuilder
    {
        /// <summary>
        /// Sets the column data type.
        /// </summary>
        /// <param name="type">The .NET data type.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasType(Type type);

        /// <summary>
        /// Sets the column as required (NOT NULL).
        /// </summary>
        /// <param name="isRequired">Whether to allow null values.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder IsRequired(bool isRequired = true);

        /// <summary>
        /// Sets the column as optional (NULL allowed).
        /// </summary>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder IsOptional();

        // Type Information - DDL Standard
        /// <summary>
        /// Sets the column type (SQL type).
        /// </summary>
        /// <param name="columnType">The SQL column type.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasColumnType(string columnType);

        /// <summary>
        /// Sets whether the column uses Unicode encoding.
        /// </summary>
        /// <param name="isUnicode">Whether the column uses Unicode encoding.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder IsUnicode(bool isUnicode = true);

        /// <summary>
        /// Sets the precision and scale for numeric columns.
        /// </summary>
        /// <param name="precision">The precision.</param>
        /// <param name="scale">The scale (optional).</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasPrecision(int precision, int scale = 0);

        /// <summary>
        /// Sets the maximum length for string/binary columns.
        /// </summary>
        /// <param name="maxLength">The maximum length.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasMaxLength(int maxLength);

        /// <summary>
        /// Sets the default value for the column.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasDefaultValue(string defaultValue);

        // Foreign Key Information - DDL Standard
        /// <summary>
        /// Sets the foreign table reference.
        /// </summary>
        /// <param name="foreignTable">The foreign table name.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasForeignTable(string foreignTable);

        /// <summary>
        /// Sets the ON DELETE action for foreign key.
        /// </summary>
        /// <param name="onDelete">The ON DELETE action.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasOnDelete(string onDelete);

        /// <summary>
        /// Sets the ON UPDATE action for foreign key.
        /// </summary>
        /// <param name="onUpdate">The ON UPDATE action.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasOnUpdate(string onUpdate);

        /// <summary>
        /// Sets the foreign key constraint name.
        /// </summary>
        /// <param name="foreignKeyName">The foreign key constraint name.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasForeignKeyName(string foreignKeyName);

        // Computed Columns - DDL Standard
        /// <summary>
        /// Sets the computed expression for the column.
        /// </summary>
        /// <param name="computedExpression">The computed expression.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasComputedExpression(string computedExpression);

        /// <summary>
        /// Sets whether the computed column is virtual.
        /// </summary>
        /// <param name="isVirtual">Whether the computed column is virtual.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder IsVirtual(bool isVirtual = true);

        // Identity/Auto-increment - DDL Standard
        /// <summary>
        /// Sets the identity seed value.
        /// </summary>
        /// <param name="identitySeed">The identity seed value.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasIdentitySeed(int identitySeed);

        /// <summary>
        /// Sets the identity increment value.
        /// </summary>
        /// <param name="identityIncrement">The identity increment value.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasIdentityIncrement(int identityIncrement);

        /// <summary>
        /// Sets the column as identity with seed and increment.
        /// </summary>
        /// <param name="seed">The identity seed value.</param>
        /// <param name="increment">The identity increment value.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder IsIdentity(int seed = 1, int increment = 1);

        // Additional Metadata - DDL Standard
        /// <summary>
        /// Sets the column comment.
        /// </summary>
        /// <param name="comment">The column comment.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasComment(string comment);

        /// <summary>
        /// Sets the column description.
        /// </summary>
        /// <param name="description">The column description.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasDescription(string description);

        /// <summary>
        /// Sets the column collation.
        /// </summary>
        /// <param name="collation">The collation name.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasCollation(string collation);

        /// <summary>
        /// Sets the column position.
        /// </summary>
        /// <param name="position">The column position.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasPosition(int position);

        /// <summary>
        /// Sets this column as a foreign key.
        /// </summary>
        /// <param name="referencedTable">The referenced table name.</param>
        /// <param name="referencedColumn">The referenced column name.</param>
        /// <param name="constraintName">The constraint name (optional).</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasForeignKey(string referencedTable, string referencedColumn = "Id", string constraintName = null);

        /// <summary>
        /// Sets a custom extended property.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>The current IColumnBuilder instance.</returns>
        IColumnBuilder HasExtendedProperty(string key, object value);
    }
}
