namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        internal static partial class Grammar
        {
            public const string OffsetNotSupported = "Offset is not supported in this operation.";
            public const string LimitNotSupported = "Limit is not supported in this operation.";
            public const string OrderByNotSupported = "Order By is not supported in this operation.";
            public const string GroupByNotSupported = "Group By is not supported in this operation.";
            public const string HavingNotSupported = "Having is not supported in this operation.";
            public const string JoinNotSupported = "Join is not supported in this operation.";

            internal static class SqlServer
            {
                public const string DeleteIncludingJoinsNotSupported = "Delete operations on multiple tables with JOINs are not supported in SQL Server. Please execute separate DELETE statements for each table.";
            }
        }
    }
}
