namespace SharpOrm.Builder.Grammars.Table
{
    public static class ExtendedPropertyKeys
    {
        public const string ColumnType = "ColumnType";
        // Type Information - DDL Standard
        public const string IsUnicode = "IsUnicode";
        public const string Precision = "Precision";
        public const string Scale = "Scale";
        public const string MaxLength = "MaxLength";
        // Constraints - DDL Standard
        public const string IsPrimaryKey = "IsPrimaryKey";
        public const string IsUnique = "IsUnique";
        public const string DefaultValue = "DefaultValue";
        public const string CheckConstraint = "CheckConstraint";
        // Index Information - DDL Standard
        public const string ForeignTable = "ForeignTable";
        // Foreign Key Information - DDL Standard
        public const string ReferencedTable = "ReferencedTable";
        public const string ReferencedColumn = "ReferencedColumn";
        public const string OnDelete = "OnDelete";
        public const string OnUpdate = "OnUpdate";
        public const string ForeignKeyName = "ForeignKeyName";
        // Computed Columns - DDL Standard
        public const string ComputedExpression = "ComputedExpression";
        public const string IsVirtual = "IsVirtual";
        // Identity/Auto-increment - DDL Standard
        public const string IdentitySeed = "IdentitySeed";
        public const string IdentityIncrement = "IdentityIncrement";
        // Additional Metadata - DDL Standard
        public const string Comment = "Comment";
        public const string Description = "Description";
        public const string Collation = "Collation";
        public const string Position = "Position";
    }
}
