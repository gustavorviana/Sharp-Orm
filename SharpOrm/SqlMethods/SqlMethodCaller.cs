using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;

namespace SharpOrm.SqlMethods
{
    public abstract class SqlMethodCaller : SqlMemberCaller
    {
        public sealed override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member)
        {
            if (member is SqlMethodInfo funcMember) return this.GetSqlExpression(info, expression, funcMember);

            throw new NotSupportedException(Messages.Mapper.MethodRequired);
        }

        protected abstract SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method);
    }
}
