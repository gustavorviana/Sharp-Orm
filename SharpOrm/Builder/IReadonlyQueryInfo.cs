namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents read-only query information.
    /// </summary>
    public interface IReadonlyQueryInfo
    {
        /// <summary>
        /// Gets the query configuration.
        /// </summary>
        QueryConfig Config { get; }

        /// <summary>
        /// Gets the name of the database table.
        /// </summary>
        DbName TableName { get; }
    }
}
