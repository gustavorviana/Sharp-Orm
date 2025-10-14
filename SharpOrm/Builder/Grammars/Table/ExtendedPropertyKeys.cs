namespace SharpOrm.Builder.Grammars.Table
{
    public static class ExtendedPropertyKeys
    {
        /// <summary>
        /// Defines the specific type of the column (e.g., int, varchar, datetime)
        /// </summary>
        public const string ColumnType = "ColumnType";

        // Type Information - DDL Standard

        /// <summary>
        /// Indicates if the string column uses Unicode encoding (nvarchar vs varchar)
        /// </summary>
        public const string IsUnicode = "IsUnicode";

        /// <summary>
        /// Total number of digits for numeric types (e.g., decimal(10,2) has precision 10)
        /// </summary>
        public const string Precision = "Precision";

        /// <summary>
        /// Number of digits after the decimal point for numeric types (e.g., decimal(10,2) has scale 2)
        /// </summary>
        public const string Scale = "Scale";

        /// <summary>
        /// Maximum length for string or binary columns (e.g., varchar(50) has MaxLength 50)
        /// </summary>
        public const string MaxLength = "MaxLength";

        // Constraints - DDL Standard

        /// <summary>
        /// Indicates if the column is part of the primary key
        /// </summary>
        public const string IsPrimaryKey = "IsPrimaryKey";

        /// <summary>
        /// Indicates if the column has a unique constraint (no duplicate values allowed)
        /// </summary>
        public const string IsUnique = "IsUnique";

        /// <summary>
        /// Default value assigned to the column when no value is provided during insert
        /// </summary>
        public const string DefaultValue = "DefaultValue";

        /// <summary>
        /// Check constraint expression that validates data (e.g., "Age >= 18")
        /// </summary>
        public const string CheckConstraint = "CheckConstraint";

        // Index Information - DDL Standard

        /// <summary>
        /// Specifies the foreign table name for index relationships
        /// </summary>
        public const string ForeignTable = "ForeignTable";

        // Foreign Key Information - DDL Standard

        /// <summary>
        /// Name of the table that this foreign key references
        /// </summary>
        public const string ReferencedTable = "ReferencedTable";

        /// <summary>
        /// Name of the column in the referenced table
        /// </summary>
        public const string ReferencedColumn = "ReferencedColumn";

        /// <summary>
        /// Action to perform when the referenced record is deleted (CASCADE, SET NULL, RESTRICT, etc.)
        /// </summary>
        public const string OnDelete = "OnDelete";

        /// <summary>
        /// Action to perform when the referenced record is updated (CASCADE, SET NULL, RESTRICT, etc.)
        /// </summary>
        public const string OnUpdate = "OnUpdate";

        /// <summary>
        /// Custom name for the foreign key constraint
        /// </summary>
        public const string ForeignKeyName = "ForeignKeyName";

        // Computed Columns - DDL Standard

        /// <summary>
        /// Expression used to calculate the computed column value (e.g., "Quantity * UnitPrice")
        /// </summary>
        public const string ComputedExpression = "ComputedExpression";

        /// <summary>
        /// Indicates if computed column is virtual (calculated on-the-fly) or persisted (stored physically)
        /// </summary>
        public const string IsVirtual = "IsVirtual";

        // Identity/Auto-increment - DDL Standard

        /// <summary>
        /// Starting value for identity/auto-increment columns (e.g., IDENTITY(1,1) has seed 1)
        /// </summary>
        public const string IdentitySeed = "IdentitySeed";

        /// <summary>
        /// Increment step for identity/auto-increment columns (e.g., IDENTITY(1,1) has increment 1)
        /// </summary>
        public const string IdentityIncrement = "IdentityIncrement";

        // Additional Metadata - DDL Standard

        /// <summary>
        /// Comment or remark about the column (metadata only, not enforced)
        /// </summary>
        public const string Comment = "Comment";

        /// <summary>
        /// Detailed description of the column's purpose and usage
        /// </summary>
        public const string Description = "Description";

        /// <summary>
        /// Collation rule for string comparison and sorting (e.g., "Latin1_General_CI_AS")
        /// </summary>
        public const string Collation = "Collation";

        /// <summary>
        /// Ordinal position of the column in the table (1-based index)
        /// </summary>
        public const string Position = "Position";
    }
}
