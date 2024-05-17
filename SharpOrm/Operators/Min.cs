namespace SharpOrm.Operators
{
    public class Min : Column
    {
        public Min(string column) : base(SqlExpression.Make("MIN(", column, ")"))
        {
        }

        public Min(string column, string alias) : base(SqlExpression.Make("MIN(", column, ") ", alias))
        {
        }
    }
}
