using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.ForeignKey;
using System.Linq.Expressions;

namespace BaseTest.Utils
{
    public class SqlMethodMapTest(QueryConfig config)
    {
        protected readonly IReadonlyQueryInfo info = new ReadonlyQueryInfo(config, new DbName());

        internal ExpressionColumn ParseColumn<T>(Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            return ParseColumns(expression, config).First();
        }

        internal ExpressionProcessor<T> GetProcessor<T>(ExpressionConfig config = ExpressionConfig.All, IForeignKeyNode? parent = null)
        {
            return new ExpressionProcessor<T>(info, parent?.RootInfo?.Config?.Translation, config, parent);
        }

        internal IEnumerable<ExpressionColumn> ParseColumns<T>(Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            return GetProcessor<T>(config).ParseColumns(expression);
        }

        protected static QueryInfo GetQueryInfo<T>(QueryConfig config, DbName name = default)
        {
            var info = new QueryInfo(config, name);
            return info;
        }
    }
}
