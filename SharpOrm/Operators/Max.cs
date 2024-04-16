namespace SharpOrm.Operators
{
    public class Max : Column
    {
        public Max(string column) : base(new SqlExpression($"MAX({column})"))
        {
        }

        public Max(string column, string alias) : base(new SqlExpression($"MAX({column}) {alias}"))
        {
        }
    }
}
