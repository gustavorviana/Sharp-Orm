using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;

namespace SharpOrm.SqlMethods
{
    public abstract class SqlMemberCaller
    {
        public abstract SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member);
    }
}
