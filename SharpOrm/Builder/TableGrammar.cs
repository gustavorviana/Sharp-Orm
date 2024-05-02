using System.Data;
using System.Linq;

namespace SharpOrm.Builder
{
    public abstract class TableGrammar
    {
        protected readonly IReadonlyQueryInfo queryInfo;
        public TableSchema Schema { get; }
        public QueryConfig Config => this.queryInfo.Config;

        /// <summary>
        /// Database name in the database's standard format.
        /// </summary>
        public abstract DbName Name { get; }

        public TableGrammar(QueryConfig config, TableSchema schema)
        {
            this.Schema = schema;
            this.queryInfo = new ReadonlyQueryInfo(config, this.Name);
        }

        public abstract SqlExpression Create();

        public abstract SqlExpression Drop();

        public abstract SqlExpression Count();

        protected ColumnTypeMap GetCustomColumnTypeMap(DataColumn column)
        {
            return this.queryInfo
                .Config
                .CustomColumnTypes?
                .FirstOrDefault(x => x.CanWork(column.DataType));
        }

        protected QueryConstructor GetConstructor()
        {
            return new QueryConstructor(queryInfo);
        }
    }
}
