namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class Connection
        {
            public const string ManagementLockedByTransaction = "It's not possible to alter the connection management when a transaction is being used.";

            public const string TransactionAlreadyOpen = "There is already an open transaction.";

            public const string UsingClosedReader = "It is not possible to use a closed DbDataReader.\"";

            public const string SetDefaultConnectionToStaticTransaction = "It's not possible to start a transaction without setting a value for ConnectionCreator.Default.";
        }
    }
}
