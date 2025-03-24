using SharpOrm.Builder.Expressions;
using SharpOrm.Msg;
using System;

namespace SharpOrm.Builder
{
    internal class ReadonlyQueryInfo : IReadonlyQueryInfo, IRootTypeMap
    {
        Type IRootTypeMap.RootType { get; set; }
        public QueryConfig Config { get; }

        public DbName TableName { get; }

        public ReadonlyQueryInfo(QueryInfo info) : this(info.Config, info.TableName)
        {
        }

        public ReadonlyQueryInfo(QueryConfig config, DbName tableName)
        {
            this.Config = config ?? throw new ArgumentNullException(Messages.ConfigMustBeNotNull);
            this.TableName = tableName;
        }
    }
}
