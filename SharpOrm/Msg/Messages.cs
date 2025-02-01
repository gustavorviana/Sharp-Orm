namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public const string OperationCannotBePerformedArgumentsMismatch = "The operation cannot be performed because the arguments passed in the SQL query do not match the provided parameters.";
        public const string InsertedTypeMismatch = "Inserted type is not the same as defined in the primary key column of the model.";
        public const string MultipleColumnsException = "For this operation, only one column must be inserted. \"*\" is not allowed.";
        public const string InsertValuesMismatch = "The number of inserted values is not the same number of primary keys.";
        public const string SourceQueryEqualToProvidedQuery = "The source query is equal to the provided query.";
        public const string OneItemOnly = "For this operation, it is necessary to apply Query.Limit = 1.";
        public const string MissingPrimaryKey = "No primary key has been configured in the model.";
        public const string ColumnsNotFound = "Columns inserted to be updated were not found.";
        public const string AtLeastOneColumnRequired = "At least one column must be entered.";
        public const string MissingCreator = "A connection builder must have been defined.";
        public const string NoColumnsInserted = "At least one column must be inserted.";
        public const string CannotUseEmptyCase = "You cannot use an empty case.";
        public const string TransactionOpen = "The transaction has already been opened.";
        public const string TransactionNotOpen = "The transaction has not been opened.";

        public const string EmptyColumnName = "Column name cannot be empty.";

        public static class Mapper
        {
            public const string NotSupported = "The type \"{0}\" was not mapped with the function/property \"{1}\".";

            public const string PropertyRequired = "The expected member for this operation is a property.";
            public const string MethodRequired = "The expected member for this operation is a method.";
        }

        public static class Expressions
        {
            public const string FunctionDisabled = "It is not possible to use functions in this operation.";
            public const string SubmembersDisabled = "It is not possible to use the subproperties \"x => x.MainProp.SubProp...\".";

            public const string NewExpressionDisabled = "The expression \"new { }\" is not supported.";
            public const string NativeTypeInTableName = "The type of the table name cannot be a native type.";
        }

        public static class Query
        {
            public const string DuplicateJoin = "The join using the property \"{0}\" has already been added.";
        }
    }
}
