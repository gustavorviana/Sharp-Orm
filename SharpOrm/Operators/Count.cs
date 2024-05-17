namespace SharpOrm.Operators
{
    public class Count : Column
    {
        public Count(string column) : base(SqlExpression.Make("COUNT(", column, ")"))
        {
        }

        public Count(string column, string alias) : base(SqlExpression.Make("COUNT(", column, ") ", alias, ""))
        {
        }
    }
}
