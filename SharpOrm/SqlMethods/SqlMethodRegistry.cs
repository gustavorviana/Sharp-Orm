using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.SqlMethods
{
    public class SqlMethodRegistry
    {
        private readonly List<SqlMemberCaller> callers = new List<SqlMemberCaller>();

        public SqlMethodRegistry Add(SqlMemberCaller caller)
        {
            callers.Add(caller);
            return this;
        }

        public SqlExpression ApplyMember(IReadonlyQueryInfo info, SqlMember property, bool forcePrefix)
        {
            SqlExpression column = GetMemberNameExpression(info, property, forcePrefix);

            foreach (var member in property.Childs)
                column = this.ApplyCaller(info, column, member);

            return new QueryBuilder(info).Add(column).ToExpression();
        }

        private SqlExpression GetMemberNameExpression(IReadonlyQueryInfo info, SqlMember property, bool forcePrefix)
        {
            if (property.IsNativeType)
                return GetNativeTypeExpression(info, property, forcePrefix);

            return GetForeignMemberExpression(info, property);
        }

        private SqlExpression GetNativeTypeExpression(IReadonlyQueryInfo info, SqlMember member, bool forcePrefix)
        {
            if (member.IsStatic || member.Member.MemberType == System.Reflection.MemberTypes.Method)
                return new SqlExpression("");

            return new DeferredMemberColumn(info, member.GetInfo(), forcePrefix);
        }

        private static DeferredMemberColumn GetForeignMemberExpression(IReadonlyQueryInfo info, SqlMember property)
        {
            if (property.Childs.Length == 0)
                throw new ForeignMemberException(property.Member, "A property of the foreign class must be provided.");

            var member = property.Childs[0];
            property.Childs = property.Childs.Skip(1).ToArray();

            return new DeferredMemberColumn(info, (SqlPropertyInfo)member, true);
        }

        private SqlExpression ApplyCaller(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member)
        {
            var caller = callers.FirstOrDefault(x => x.CanWork(member));
            if (caller == null)
                throw new NotSupportedException(string.Format(Messages.Mapper.NotSupported, member.DeclaringType, member.Name));

            return caller.GetSqlExpression(info, expression, member);
        }
    }
}
