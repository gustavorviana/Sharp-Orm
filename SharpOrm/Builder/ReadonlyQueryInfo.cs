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
            this.Config = config;
            this.TableName = tableName;
        }
    }
}
