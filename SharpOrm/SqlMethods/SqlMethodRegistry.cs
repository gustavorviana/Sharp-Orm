using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using SharpOrm.SqlMethods.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public SqlExpression ApplyMember(IReadonlyQueryInfo info, SqlMember property, out bool isFk)
        {
            SqlExpression column = GetMemberNameExpression(info, property, out isFk);

            foreach (var member in property.Childs)
                column = this.ApplyCaller(info, column, member);

            return column;
        }

        private SqlExpression GetMemberNameExpression(IReadonlyQueryInfo info, SqlMember property, out bool isFk)
        {
            if (property.IsNativeType)
            {
                isFk = false;
                return new SqlExpression(property.IsStatic ? "" : info.Config.ApplyNomenclature(property.Name));
            }

            if (!(info is QueryInfo qInfo) || !(qInfo.Joins.FirstOrDefault(x => x.MemberInfo == property.Member) is JoinQuery join))
                throw ForeignMemberException.IncompatibleType(property.Member);

            if (property.Childs.Length == 0)
                throw new ForeignMemberException(property.Member, "A property of the foreign class must be provided.");

            var prefix = join.Info.TableName.TryGetAlias(info.Config);
            var member = property.Childs[0];

            property.Childs = property.Childs.Skip(1).ToArray();


            isFk = true;
            return new SqlExpression($"{prefix}.{info.Config.ApplyNomenclature(member.Name)}");
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
