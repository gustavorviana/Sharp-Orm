namespace SharpOrm.Operators
{
    public class Avg : Column
    {
        public Avg(string column) : base(SqlExpression.Make("AVG(", column, ")"))
        {
        }

        public Avg(string column, string alias) : base(SqlExpression.Make("AVG(", column, ") ", alias))
        {
        }
    }
}
