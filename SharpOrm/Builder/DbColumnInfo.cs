using System;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents metadata information about a database column.
    /// This class is read-only and stores information loaded directly from the database schema.
    /// </summary>
    public class DbColumnInfo : IDbColumnInfo
    {
        /// <summary>
        /// Gets the name of the column in the database.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets the database-specific data type of the column (e.g., "varchar", "int", "datetime2").
        /// </summary>
        public string DataType { get; }

        /// <summary>
        /// Gets the maximum length of the column for string/binary types.
        /// Returns -1 for MAX or unlimited length, null if not applicable.
        /// </summary>
        public int? MaxLength { get; }

        /// <summary>
        /// Gets the numeric precision for decimal/numeric types.
        /// Null if not applicable.
        /// </summary>
        public int? Precision { get; }

        /// <summary>
        /// Gets the numeric scale for decimal/numeric types.
        /// Null if not applicable.
        /// </summary>
        public int? Scale { get; }

        /// <summary>
        /// Gets whether the column allows NULL values.
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// Gets whether the column is part of the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; }

        /// <summary>
        /// Gets whether the column is an identity/auto-increment column.
        /// </summary>
        public bool IsIdentity { get; }

        /// <summary>
        /// Gets whether the column is computed/generated.
        /// </summary>
        public bool IsComputed { get; }

        /// <summary>
        /// Gets the default value expression for the column, if any.
        /// </summary>
        public string DefaultValue { get; }

        /// <summary>
        /// Gets the ordinal position of the column in the table (1-based).
        /// </summary>
        public int OrdinalPosition { get; }

        /// <summary>
        /// Gets the collation name for character columns.
        /// Null if not applicable.
        /// </summary>
        public string Collation { get; }

        /// <summary>
        /// Gets additional database-specific metadata.
        /// </summary>
        public string Comment { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbColumnInfo"/> class.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="dataType">The database-specific data type.</param>
        /// <param name="isNullable">Whether the column allows NULL values.</param>
        /// <param name="ordinalPosition">The ordinal position of the column in the table.</param>
        /// <param name="maxLength">The maximum length for string/binary types.</param>
        /// <param name="precision">The numeric precision.</param>
        /// <param name="scale">The numeric scale.</param>
        /// <param name="isPrimaryKey">Whether the column is part of the primary key.</param>
        /// <param name="isIdentity">Whether the column is an identity/auto-increment column.</param>
        /// <param name="isComputed">Whether the column is computed/generated.</param>
        /// <param name="defaultValue">The default value expression.</param>
        /// <param name="collation">The collation name.</param>
        /// <param name="comment">Column comment or description.</param>
        public DbColumnInfo(
            string columnName,
            string dataType,
            bool isNullable,
            int ordinalPosition,
            int? maxLength = null,
            int? precision = null,
            int? scale = null,
            bool isPrimaryKey = false,
            bool isIdentity = false,
            bool isComputed = false,
            string defaultValue = null,
            string collation = null,
            string comment = null)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("Data type cannot be null or empty.", nameof(dataType));

            ColumnName = columnName;
            DataType = dataType;
            IsNullable = isNullable;
            OrdinalPosition = ordinalPosition;
            MaxLength = maxLength;
            Precision = precision;
            Scale = scale;
            IsPrimaryKey = isPrimaryKey;
            IsIdentity = isIdentity;
            IsComputed = isComputed;
            DefaultValue = defaultValue;
            Collation = collation;
            Comment = comment;
        }

        /// <summary>
        /// Gets the full type definition including size/precision information.
        /// </summary>
        /// <returns>A string representation of the full column type.</returns>
        public string GetFullTypeDefinition()
        {
            if (MaxLength.HasValue)
            {
                if (MaxLength.Value == -1)
                    return $"{DataType}(MAX)";

                return $"{DataType}({MaxLength.Value})";
            }

            if (Precision.HasValue)
            {
                if (Scale.HasValue)
                    return $"{DataType}({Precision.Value},{Scale.Value})";

                return $"{DataType}({Precision.Value})";
            }

            return DataType;
        }

        /// <summary>
        /// Returns a string representation of the column information.
        /// </summary>
        public override string ToString()
        {
            var nullable = IsNullable ? "NULL" : "NOT NULL";
            var pk = IsPrimaryKey ? " PRIMARY KEY" : "";
            var identity = IsIdentity ? " IDENTITY" : "";

            return $"{ColumnName} {GetFullTypeDefinition()} {nullable}{pk}{identity}";
        }
    }
}
