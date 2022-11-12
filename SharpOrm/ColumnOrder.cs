namespace SharpOrm
{
    public class ColumnOrder
    {
        public OrderBy Order { get; }
        public Column Column { get; }

        public ColumnOrder(string column, OrderBy order) : this(new Column(column), order)
        {

        }

        public ColumnOrder(Column column, OrderBy order)
        {
            this.Column = column;
            this.Order = order;
        }
    }
}
