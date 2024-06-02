namespace SharpOrm.Operators
{
    /// <summary>
    /// Represents a SQL MIN function.
    /// </summary>
    public class Min : Column
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Min"/> class for the specified column.
        /// </summary>
        /// <param name="column">The column to find the minimum value of.</param>
        public Min(string column) : base(SqlExpression.Make("MIN(", column, ")"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Min"/> class for the specified column with an alias.
        /// </summary>
        /// <param name="column">The column to find the minimum value of.</param>
        /// <param name="alias">The alias for the minimum value result.</param>
        public Min(string column, string alias) : base(SqlExpression.Make("MIN(", column, ") ", alias))
        {
        }
    }
}
