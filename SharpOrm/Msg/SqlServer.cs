namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class SqlServer
        {
            public const string InvalidTempTableName = "A temporary table cannot contain '.' in its name.";
            public const string SchemaNameOverflow = "The table name must contain up to 115 characters.";
            public const string OffsetWithSortNotSuported = "Cannot use Query.Offset without defining a sort column.";
        }
    }
}
