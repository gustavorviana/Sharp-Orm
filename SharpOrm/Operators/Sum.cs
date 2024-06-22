namespace SharpOrm.Operators
{
    /// <summary>
    /// Represents a SQL SUM function.
    /// </summary>
    public class Sum : Column
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Sum"/> class for the specified column.
        /// </summary>
        /// <param name="column">The column to sum the values of.</param>
        public Sum(string column) : base(SqlExpression.Make("SUM(", column, ")"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sum"/> class for the specified column with an alias.
        /// </summary>
        /// <param name="column">The column to sum the values of.</param>
        /// <param name="alias">The alias for the sum result.</param>
        public Sum(string column, string alias) : base(SqlExpression.Make("SUM(", column, ") ", alias))
        {
        }
    }
}
