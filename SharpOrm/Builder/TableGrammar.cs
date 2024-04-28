using System.Data;
using System.Linq;

namespace SharpOrm.Builder
{
    public abstract class TableGrammar
    {
        protected readonly IReadonlyQueryInfo queryInfo;

        public TableGrammar(IReadonlyQueryInfo queryInfo)
        {
            this.queryInfo = queryInfo;
        }

        public abstract SqlExpression Create(TableSchema table);

        public abstract SqlExpression Drop(TableSchema table);

        public abstract SqlExpression Count(TableSchema table);

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

        public abstract DbName GetName(TableSchema schema);
    }
}
