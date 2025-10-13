namespace SharpOrm.Connection
{
    /// <summary>
    /// Manages database connection transactions, providing commit and rollback operations.
    /// </summary>
    public interface ITransaction
    {
        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <returns>True if the commit was successful; otherwise, false.</returns>
        bool Commit();

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        /// <returns>True if the rollback was successful; otherwise, false.</returns>
        bool Rollback();
    }
}