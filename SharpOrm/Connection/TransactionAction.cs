namespace SharpOrm.Connection
{
    /// <summary>
    /// Represents a method that will be called within the context of a transaction.
    /// </summary>
    /// <param name="manager">The connection manager.</param>
    public delegate void TransactionAction(ConnectionManager manager);

    /// <summary>
    /// Represents a method that will be called within the context of a transaction and returns a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="manager">The connection manager.</param>
    /// <returns>The result of the transaction action.</returns>
    public delegate T TransactionAction<T>(ConnectionManager manager);

}