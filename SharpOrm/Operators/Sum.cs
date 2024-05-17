namespace SharpOrm.Operators
{
    public class Sum : Column
    {
        public Sum(string column) : base(SqlExpression.Make("SUM(", column, ")"))
        {
        }

        public Sum(string column, string alias) : base(SqlExpression.Make("SUM(", column, ") ", alias))
        {
        }
    }
}
