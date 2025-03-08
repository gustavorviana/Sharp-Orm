namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class Sqlite
        {
            public const string DeleteWithAliasNotSupported = "SQLite does not support executing a DELETE with a table alias.";
            public const string OperationNotSupported = "SQLite does not support \"{0}\" with \"{1}\".";
            public const string NotSupported = "SQLite does not support {0}.";
            public const string MultiplePrimaryKeyWithAutoIncrementError = "It is not possible to have more than one primary key column when there is an AUTOINCREMENT column.";
            public const string MultipleAutoIncrementError = "There is no support for more than one column with AUTOINCREMENT.";
        }
    }
}
