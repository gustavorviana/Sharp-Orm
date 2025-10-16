namespace SharpOrm.Builder
{
    interface IDbColumnInfo
    {

        /// <summary>
        /// Gets the name of the column in the database.
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Gets the database-specific data type of the column (e.g., "varchar", "int", "datetime2").
        /// </summary>
        string DataType { get; }

        /// <summary>
        /// Gets the maximum length of the column for string/binary types.
        /// Returns -1 for MAX or unlimited length, null if not applicable.
        /// </summary>
        int? MaxLength { get; }

        /// <summary>
        /// Gets the numeric precision for decimal/numeric types.
        /// Null if not applicable.
        /// </summary>
        int? Precision { get; }

        /// <summary>
        /// Gets the numeric scale for decimal/numeric types.
        /// Null if not applicable.
        /// </summary>
        int? Scale { get; }

        /// <summary>
        /// Gets whether the column allows NULL values.
        /// </summary>
        bool IsNullable { get; }

        /// <summary>
        /// Gets whether the column is part of the primary key.
        /// </summary>
        bool IsPrimaryKey { get; }

        /// <summary>
        /// Gets whether the column is an identity/auto-increment column.
        /// </summary>
        bool IsIdentity { get; }

        /// <summary>
        /// Gets whether the column is computed/generated.
        /// </summary>
        bool IsComputed { get; }

        /// <summary>
        /// Gets the default value expression for the column, if any.
        /// </summary>
        string DefaultValue { get; }

        /// <summary>
        /// Gets the ordinal position of the column in the table (1-based).
        /// </summary>
        int OrdinalPosition { get; }

        /// <summary>
        /// Gets the collation name for character columns.
        /// Null if not applicable.
        /// </summary>
        string Collation { get; }

        /// <summary>
        /// Gets additional database-specific metadata.
        /// </summary>
        string Comment { get; }

        /// <summary>
        /// Gets the full type definition including size/precision information.
        /// </summary>
        /// <returns>A string representation of the full column type.</returns>
        string GetFullTypeDefinition();
    }
}