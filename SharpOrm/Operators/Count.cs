namespace SharpOrm.Operators
{
    public class Count : Column
    {
        public Count(string column) : base(new SqlExpression($"COUNT({column})"))
        {
        }

        public Count(string column, string alias) : base(new SqlExpression($"COUNT({column}) {alias}"))
        {
        }
    }
}
