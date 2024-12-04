using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;

namespace SharpOrm.SqlMethods
{
    public abstract class SqlMethodCaller : SqlMemberCaller
    {
        public override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member)
        {
            if (member is SqlMethodInfo funcMember) return this.GetSqlExpression(info, expression, funcMember.Args);

            throw new NotSupportedException(Messages.Mapper.MethodRequired);
        }

        public abstract SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values);
    }
}
