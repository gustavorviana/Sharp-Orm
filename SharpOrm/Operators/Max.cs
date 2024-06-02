namespace SharpOrm.Operators
{
    /// <summary>
    /// Represents a SQL MAX function.
    /// </summary>
    public class Max : Column
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Max"/> class for the specified column.
        /// </summary>
        /// <param name="column">The column to find the maximum value of.</param>
        public Max(string column) : base(SqlExpression.Make("MAX(", column, ")"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Max"/> class for the specified column with an alias.
        /// </summary>
        /// <param name="column">The column to find the maximum value of.</param>
        /// <param name="alias">The alias for the maximum value result.</param>
        public Max(string column, string alias) : base(SqlExpression.Make("MAX(", column, ") ", alias))
        {
        }
    }

}
