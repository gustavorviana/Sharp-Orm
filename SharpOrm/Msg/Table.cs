using SharpOrm.Connection;

namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class Table
        {
            public const string ColumnAlreadyMapped = "It is not possible to alter the column; it has already been built.";

            public const string CannotChangeAfterBuild = "It is not possible to change the table after it has been built.";

            public const string NotBuilded = "Table not built yet.";
            public const string AlreadyMapped = "The type has already been mapped.";

            public const string TableNotFound = "The table '{0}' was not found.";

            public static readonly string InvalidEmpTableConnection = $"To use a temporary table, it is necessary to configure the connection to \"{nameof(ConnectionManagement)}.{nameof(ConnectionManagement.LeaveOpen)}\" or \"{nameof(ConnectionManagement)}.{nameof(ConnectionManagement.CloseOnManagerDispose)}\".";

            public const string GetUnmappedColumn = "It is not possible to retrieve the column \"{0}\", it was defined as unmapped.";

            public const string UnsupportedType = "Unsupported data type: {0}.";

            public const string InvalidType = "Invalid type provided.";

            public const string KeyNotFound = "The key '{0}' does not exist in the object '{1}'.";

            public const string MemberTypeNotSupported = "It is not possible to map the member \"{0}\" of type \"{1}\".";

            public const string ColumnNotFound = "The column \"{0}\" was not found.";
        }
    }
}
