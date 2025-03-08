namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
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
            public const string FirstCharInvalid = "The table name cannot start with '{0}'.";

            public const string InvalidWhereValue = "Cannot add a {0} to the WHERE clause.";
        }
    }
}
