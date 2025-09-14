using SharpOrm.Msg;
using System;

namespace SharpOrm.Builder
{
    internal class ReadonlyQueryInfo : IReadonlyQueryInfo
    {
        public QueryConfig Config { get; }

        public DbName TableName { get; }

        public ReadonlyQueryInfo(QueryInfo info) : this(info.Config, info.TableName)
        {
        }

        public ReadonlyQueryInfo(QueryConfig config, DbName tableName)
        {
            Config = config ?? throw new ArgumentNullException(Messages.ConfigMustBeNotNull);
            TableName = tableName;
        }
    }
}
