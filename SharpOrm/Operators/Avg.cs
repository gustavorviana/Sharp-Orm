namespace SharpOrm.Operators
{
    /// <summary>
    /// Represents a SQL AVG (average) function.
    /// </summary>
    public class Avg : Column
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Avg"/> class for the specified column.
        /// </summary>
        /// <param name="column">The column to calculate the average for.</param>
        public Avg(string column) : base(SqlExpression.Make("AVG(", column, ")"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Avg"/> class for the specified column with an alias.
        /// </summary>
        /// <param name="column">The column to calculate the average for.</param>
        /// <param name="alias">The alias for the average calculation.</param>
        public Avg(string column, string alias) : base(SqlExpression.Make("AVG(", column, ") ", alias))
        {
        }
    }

}
