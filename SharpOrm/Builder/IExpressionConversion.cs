namespace SharpOrm.Builder
{
    public interface IExpressionConversion
    {
        SqlExpression ToExpression(IReadonlyQueryInfo info);
    }
}
