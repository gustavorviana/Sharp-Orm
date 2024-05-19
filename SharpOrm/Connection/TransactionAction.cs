namespace SharpOrm.Connection
{
    public delegate void TransactionAction(ConnectionManager manager);
    public delegate T TransactionAction<T>(ConnectionManager manager);
}