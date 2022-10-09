namespace SharpOrm.Builder
{
    public interface IExpressionConversion
    {
        SqlExpression ToExpression(QueryBase query);
    }
}
