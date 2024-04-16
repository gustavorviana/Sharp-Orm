namespace SharpOrm.Operators
{
    public class Sum : Column
    {
        public Sum(string column) : base(new SqlExpression($"SUM({column})"))
        {
        }

        public Sum(string column, string alias) : base(new SqlExpression($"SUM({column}) {alias}"))
        {
        }
    }
}
