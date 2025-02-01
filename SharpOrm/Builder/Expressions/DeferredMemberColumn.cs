using SharpOrm.SqlMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    internal class DeferredMemberColumn : SqlExpression, IDeferredSqlExpression
    {
        private readonly IReadonlyQueryInfo queryInfo;
        private readonly SqlPropertyInfo member;
        private readonly bool needPrefix;

        public string Name => ColumnInfo.GetName(member.Member);
        private string tableName = null;

        public DeferredMemberColumn(IReadonlyQueryInfo queryInfo, SqlPropertyInfo member, bool needPrefix)
        {
            Parameters = new object[0];
            this.needPrefix = needPrefix;
            this.queryInfo = queryInfo;
            this.member = member;
        }

        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            return new SqlExpression(ToString(GetExpectedQueryInfo(info)));
        }

        public override string ToString()
        {
            return ToString(queryInfo);
        }

        private string ToString(IReadonlyQueryInfo queryInfo)
        {
            StringBuilder builder = new StringBuilder();

            if (needPrefix || queryInfo.NeedPrefix())
                builder.Append(queryInfo.GetColumnPrefix()).Append('.');

            builder.Append(queryInfo.Config.ApplyNomenclature(Name));

            return builder.ToString();
        }

        private IReadonlyQueryInfo GetExpectedQueryInfo(IReadonlyQueryInfo parentQuery)
        {
            if (!NeedChangeInfo(parentQuery))
                return queryInfo;

            if (!(queryInfo is QueryInfo qInfo))
                return parentQuery;

            if (qInfo.Joins.FirstOrDefault(IsMemberJoin) is JoinQuery join)
                return join.Info;

            throw ForeignMemberException.JoinNotFound(member, GetTableName());
        }

        private bool NeedChangeInfo(IReadonlyQueryInfo info)
        {
            return !(queryInfo is QueryInfo qInfo) 
                || (!qInfo.IsExpectedType(member.DeclaringType)
                && !qInfo.TableName.Name.Equals(GetTableName(), StringComparison.OrdinalIgnoreCase));
        }

        private bool IsMemberJoin(JoinQuery join)
        {
            return join.MemberInfo == member.Member || join.Info.TableName.Name == GetTableName();
        }

        private string GetTableName()
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = queryInfo.Config.Translation.GetTableName(member.DeclaringType);

            return tableName;
        }
    }
}
