namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class TableMap
        {
            public const string ColumnAlreadyMapped = "It is not possible to alter the column; it has already been built.";

            public const string CannotChangeAfterBuild = "It is not possible to change the table after it has been built.";

            public const string NotBuilded = "Table not built yet.";
            public const string AlreadyMapped = "The type has already been mapped.";

            public static string AlreadMapped { get; internal set; }
        }
    }
}
