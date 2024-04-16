namespace SharpOrm.Operators
{
    public class Avg : Column
    {
        public Avg(string column) : base(new SqlExpression($"AVG({column})"))
        {
        }

        public Avg(string column, string alias) : base(new SqlExpression($"AVG({column}) {alias}"))
        {
        }
    }
}
