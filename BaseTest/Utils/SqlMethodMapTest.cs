using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System.Linq.Expressions;

namespace BaseTest.Utils
{
    public class SqlMethodMapTest(QueryConfig config)
    {
        protected readonly IReadonlyQueryInfo info = new ReadonlyQueryInfo(config, new DbName());

        internal ExpressionColumn ParseColumn<T>(Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            ((IRootTypeMap)info).RootType = typeof(T);
            return ParseColumns(expression, config).First();
        }

        internal ExpressionProcessor<T> GetProcessor<T>(ExpressionConfig config = ExpressionConfig.All)
        {
            ((IRootTypeMap)info).RootType = typeof(T);
            return new ExpressionProcessor<T>(info, config);
        }

        internal IEnumerable<ExpressionColumn> ParseColumns<T>(Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            return GetProcessor<T>(config).ParseColumns(expression);
        }

        protected static QueryInfo GetQueryInfo<T>(QueryConfig config, DbName name = default)
        {
            var info = new QueryInfo(config, name);
            ((IRootTypeMap)info).RootType = typeof(T);
            return info;
        }
    }
}
