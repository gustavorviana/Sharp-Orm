namespace SharpOrm
{
    public class ValueExp : SqlExpression
    {
        public ValueExp(object value) : base("?", value)
        {
        }
    }
}
