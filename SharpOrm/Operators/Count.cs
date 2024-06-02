namespace SharpOrm.Operators
{
    /// <summary>
    /// Represents a SQL COUNT function.
    /// </summary>
    public class Count : Column
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Count"/> class for the specified column.
        /// </summary>
        /// <param name="column">The column to count.</param>
        public Count(string column) : base(SqlExpression.Make("COUNT(", column, ")"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Count"/> class for the specified column with an alias.
        /// </summary>
        /// <param name="column">The column to count.</param>
        /// <param name="alias">The alias for the count result.</param>
        public Count(string column, string alias) : base(SqlExpression.Make("COUNT(", column, ") ", alias, ""))
        {
        }
    }

}
