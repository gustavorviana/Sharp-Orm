namespace SharpOrm.Operators
{
    public class Max : Column
    {
        public Max(string column) : base(SqlExpression.Make("MAX(", column, ")"))
        {
        }

        public Max(string column, string alias) : base(SqlExpression.Make("MAX(", column, ") ", alias))
        {
        }
    }
}
