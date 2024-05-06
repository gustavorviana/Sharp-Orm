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
        public virtual DbName Name { get; }
        protected QueryInfo BasedTable => this.Schema.BasedQuery.Info;

        public TableGrammar(QueryConfig config, TableSchema schema)
        {
            this.Schema = schema;
            this.Name = new DbName(schema.Name);
            this.queryInfo = new ReadonlyQueryInfo(config, this.Name);
        }

        public abstract SqlExpression Create();

        protected void WriteColumns(QueryConstructor query, Column[] columns)
        {
            if (columns.Length == 0)
            {
                query.Add("*");
                return;
            }

            query.AddExpression(columns[0]);

            for (int i = 1; i < columns.Length; i++)
                query.Add(",").AddExpression(columns[i]);
        }

        public abstract SqlExpression Drop();

        public abstract SqlExpression Exists();

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
