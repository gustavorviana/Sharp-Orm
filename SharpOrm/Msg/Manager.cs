namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class Manager
        {
            public const string MananementLockedByTransaction = "It's not possible to alter the connection management when a transaction is being used.";

            public const string TransactionAlreadyOpen = "There is already an open transaction.";
        }
    }
}
