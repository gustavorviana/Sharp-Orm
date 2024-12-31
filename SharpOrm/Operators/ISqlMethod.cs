using SharpOrm.Builder;

namespace SharpOrm.Operators
{
    public interface ISqlMethod
    {
        SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values);
    }
}
