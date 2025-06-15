using SharpOrm.SqlMethods;
using System;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    internal class DeferredMemberColumn : SqlExpression, IDeferredSqlExpression
    {
        private readonly IReadonlyQueryInfo _queryInfo;
        private readonly SqlPropertyInfo _member;
        private readonly bool _needPrefix;

        public string Name => ColumnInfo.GetName(_member.Member);
        private string tableName = null;

        public DeferredMemberColumn(IReadonlyQueryInfo queryInfo, SqlPropertyInfo member, bool needPrefix)
        {
            Parameters = DotnetUtils.EmptyArray<object>();
            _needPrefix = needPrefix;
            _queryInfo = queryInfo;
            _member = member;
        }

        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            return new SqlExpression(ToString(GetExpectedQueryInfo(info)));
        }

        public override string ToString()
        {
            return ToString(_queryInfo);
        }

        private string ToString(IReadonlyQueryInfo queryInfo)
        {
            StringBuilder builder = new StringBuilder();

            if (_needPrefix || queryInfo.NeedPrefix())
                builder.Append(queryInfo.GetColumnPrefix()).Append('.');

            builder.Append(queryInfo.Config.ApplyNomenclature(Name));

            return builder.ToString();
        }

        private IReadonlyQueryInfo GetExpectedQueryInfo(IReadonlyQueryInfo parentQuery)
        {
            if (!NeedChangeInfo(parentQuery))
                return _queryInfo;

            if (!(_queryInfo is QueryInfo qInfo))
                return parentQuery;

            if (qInfo.Joins.FirstOrDefault(IsMemberJoin) is JoinQuery join)
                return join.Info;

            throw ForeignMemberException.JoinNotFound(_member, GetTableName());
        }

        private bool NeedChangeInfo(IReadonlyQueryInfo info)
        {
            return !(_queryInfo is QueryInfo qInfo)
                || (!qInfo.IsExpectedType(_member.DeclaringType)
                && !qInfo.TableName.Name.Equals(GetTableName(), StringComparison.OrdinalIgnoreCase));
        }

        private bool IsMemberJoin(JoinQuery join)
        {
            return join.MemberInfo == _member.Member || join.Info.TableName.Name == GetTableName();
        }

        private string GetTableName()
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = _queryInfo.Config.Translation.GetTableName(_member.DeclaringType);

            return tableName;
        }
    }
}
