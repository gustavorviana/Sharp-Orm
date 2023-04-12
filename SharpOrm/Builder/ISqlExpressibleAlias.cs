namespace SharpOrm.Builder
{
    public interface ISqlExpressibleAlias : ISqlExpressible
    {
        SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias);
    }
}
