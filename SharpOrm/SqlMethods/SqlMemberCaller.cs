using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;

namespace SharpOrm.SqlMethods
{
    public abstract class SqlMemberCaller
    {
        public abstract bool CanWork(SqlMemberInfo member);

        public abstract SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member);
    }
}
