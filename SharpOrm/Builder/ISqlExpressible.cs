namespace SharpOrm.Builder
{
    public interface ISqlExpressible
    {
        SqlExpression ToExpression(IReadonlyQueryInfo info);
    }
}
