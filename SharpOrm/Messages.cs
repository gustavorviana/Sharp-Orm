namespace SharpOrm
{
    internal static class Messages
    {
        public const string OperationCannotBePerformedArgumentsMismatch = "The operation cannot be performed because the arguments passed in the SQL query do not match the provided parameters.";
        public const string InsertedTypeMismatch = "Inserted type is not the same as defined in the primary key column of the model.";
        public const string InsertValuesMismatch = "The number of inserted values is not the same number of primary keys.";
        public const string SourceQueryEqualToProvidedQuery = "The source query is equal to the provided query.";
        public const string MissingPrimaryKey = "No primary key has been configured in the model.";
        public const string ColumnsNotFound = "Columns inserted to be updated were not found.";
        public const string AtLeastOneColumnRequired = "At least one column must be entered.";
        public const string MissingCreator = "A connection builder must have been defined.";
        public const string NoColumnsInserted = "At least one column must be inserted.";
        public const string CannotUseEmptyCase = "You cannot use an empty case.";
    }
}
