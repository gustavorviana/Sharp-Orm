using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.SqlMethods.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public SqlExpression ApplyMember(IReadonlyQueryInfo info, SqlMember property)
        {
            SqlExpression column = new SqlExpression(property.IsStatic ? "" : info.Config.ApplyNomenclature(property.Name));

            foreach (var member in property.GetChilds())
                column = this.ApplyCaller(info, column, member);

            return column;
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
