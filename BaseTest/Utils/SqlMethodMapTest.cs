using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System.Linq.Expressions;

namespace BaseTest.Utils
{
    public class SqlMethodMapTest(QueryConfig config)
    {
        protected readonly IReadonlyQueryInfo info = new ReadonlyQueryInfo(config, new DbName());

        internal Column ParseColumn<T>(Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            return ParseColumns(expression, config).First();
        }

        internal ExpressionProcessor<T> GetProcessor<T>(ExpressionConfig config = ExpressionConfig.All)
        {
            return new ExpressionProcessor<T>(info, config);
        }

        internal IEnumerable<Column> ParseColumns<T>(Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            return this.GetProcessor<T>(config).ParseColumns(expression);
        }
    }
}
