using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;

namespace SharpOrm.SqlMethods
{
    public abstract class SqlMemberCaller<T> : SqlMemberCaller
    {
        public sealed override bool CanWork(SqlMemberInfo member)
        {
            return member.DeclaringType == typeof(T) && CanWorkMember(member);
        }

        protected abstract bool CanWorkMember(SqlMemberInfo member);

        public sealed override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member)
        {
            if (member is SqlMethodInfo funcMember) return this.GetSqlMethodExpression(info, expression, funcMember);
            if (member is SqlPropertyInfo propMember) return this.GetSqlPropertyExpression(info, expression, propMember);

            throw new NotSupportedException(string.Format(Messages.Mapper.NotSupported, member.DeclaringType, member.Name));
        }

        protected abstract SqlExpression GetSqlMethodExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method);

        protected abstract SqlExpression GetSqlPropertyExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member);
    }

    public abstract class SqlMemberCaller
    {
        public abstract bool CanWork(SqlMemberInfo member);

        public abstract SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member);
    }
}
