using static SharpOrm.Msg.Messages;

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

        public static class Manager
        {
            public const string MananementLockedByTransaction = "It's not possible to alter the connection management when a transaction is being used.";

            public const string TransactionAlreadyOpen = "There is already an open transaction.";
        }

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
            public const string ArgumentNotSupported = "Argument expression type {0} is not supported.";
            public const string MemberNotSupported = "Member expression type {0} is not supported.";
            public const string LoadIncompatible = "It's not possible to load the {0} '{1}' because its type is incompatible.";
            public const string Invalid = "The provided expression is not valid.";

            public const string NewExpressionDisabled = "The expression \"new { }\" is not supported.";
            public const string NativeTypeInTableName = "The type of the table name cannot be a native type.";

            public const string OnlyFieldsAndproerties = "Only properties and fields are supported in this operation.";
        }

        public static class Cell
        {
            public const string NumericOnly = "Only numeric types are allowed.";
        }

        public static class Translation
        {
            public const string BytesExpected = "Invalid value provided. Expected a MemoryStream or byte[].";
        }

        public static class Query
        {
            public const string DuplicateJoin = "The join using the property \"{0}\" has already been added.";
            public const string ComparisionColumnMustBeSet = "The comparison columns must be defined.";
            public const string JoinNotFound = "Join with the specified name '{0}' was not found.";

            public const string ClassNotSupportSoftDelete = "The class does not support soft delete, only those with SoftDeleteAttribute do.";
            public const string SoftNotSupported = "{0} is not supported, the object must be configured with the SoftDeleteAttribute attribute.";
            public const string InvalidOperation = "Invalid SQL operation: {0}";
            public const string SelfInstanceNotSupported = "The same instance cannot be passed as a parameter.";
            public const string ExpressionParamsNotMath = "The operation cannot be performed because the arguments passed in the SQL query do not match the provided parameters.";

            public const string ColumnTypeNotsupported = "The column type is not supported.";
            public const string ColumnsMustSuportAnyOfTypes = "The column type is invalid. Only the following types can be used: SqlExpression, ISqlExpressible, DateTime, DateTimeOffset, TimeSpan, numbers or Enum.";

            public const string EmptyColumnName = "Column name cannot be empty.";
            public const string ColumnNotSuportDot = "The column name cannot contain \".\"";

            public const string ReadOnly = "This query is read-only.";
        }

        public static class TableMap
        {
            public const string ColumnAlreadyMapped = "It is not possible to alter the column; it has already been built.";

            public const string CannotChangeAfterBuild = "It is not possible to change the table after it has been built.";

            public const string NotBuilded = "Table not built yet.";
            public const string AlreadyMapped = "The type has already been mapped.";

            public static string AlreadMapped { get; internal set; }
        }

        public static class Name
        {
            public const string InvalidNameChars = "The name contains one or more invalid characters.";
            public const string InvalidAliasChars = "The alias contains one or more invalid characters.";
            public const string InvalidTableName = "Table name is invalid.";
        }
    }
}
