namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a temporary table holding values that can be used in SQL expressions.
    /// </summary>
    public interface IDbTableValue : ISqlExpressible
    {
        /// <summary>
        /// Gets the name of the temporary table.
        /// </summary>
        DbName Table { get; }
    }
}
