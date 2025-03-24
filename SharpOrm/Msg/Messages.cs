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
        public const string EnumerableCanExecuteOnce = "IEnumerable can be executed only once.";

        public const string DynamicNotSupported = "Dynamic types are not supported in this operation.";
        public const string ConfigMustBeNotNull = "The query configuration cannot be null.";

        public const string TableManagementNotSupported = "{0} does not support creating/editing/removing tables.";

        public const string QueryConfigNotSupportOperation = "{0} does not support this operation.";

        public const string CannotConvertTo = "\"{0}\" cannot be converted to {1}.";

        public const string TypeNotSupported = "Type \"{0}\" is not supported";

        public const string NumericOnly = "Only numeric types are allowed.";

        public const string InvalidCountColumn = "The name of a column or '*' must be entered for counting.";
    }
}
