namespace SharpOrm
{
    /// <summary>
    /// Represents the order of a column in a query.
    /// </summary>
    public class ColumnOrder
    {
        /// <summary>
        /// Gets the order in which the column is sorted.
        /// </summary>
        public OrderBy Order { get; }

        /// <summary>
        /// Gets the column to be ordered.
        /// </summary>
        public Column Column { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnOrder"/> class with the specified column name and order.
        /// </summary>
        /// <param name="column">The name of the column to be ordered.</param>
        /// <param name="order">The order in which to sort the column.</param>
        public ColumnOrder(string column, OrderBy order) : this(new Column(column), order)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnOrder"/> class with the specified column and order.
        /// </summary>
        /// <param name="column">The column to be ordered.</param>
        /// <param name="order">The order in which to sort the column.</param>
        public ColumnOrder(Column column, OrderBy order)
        {
            this.Column = column;
            this.Order = order;
        }
    }

}
