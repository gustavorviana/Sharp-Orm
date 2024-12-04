using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Linq.Expressions;

namespace SharpOrm.SqlMethods
{
    public abstract class SqlPropertyCaller : SqlMemberCaller
    {
        public override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member)
        {
            if (member is SqlPropertyInfo propMember) return this.GetSqlExpression(info, expression, propMember);

            throw new NotSupportedException(Messages.Mapper.PropertyRequired);
        }

        protected abstract SqlExpression GetSqlExpression(IReadonlyQueryInfo info, Expression column, SqlPropertyInfo member);
    }
}
