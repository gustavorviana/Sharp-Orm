using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System.Linq.Expressions;

namespace BaseTest.Utils
{
    public class SqlMethodMapTest(QueryConfig config)
    {
        protected readonly IReadonlyQueryInfo info = new ReadonlyQueryInfo(config, new DbName());

        protected Column ParseColumn<T>(Expression<ColumnExpression<T>> expression, bool allowSubMembers = true)
        {
            return ParseColumns(expression, allowSubMembers).First();
        }

        protected IEnumerable<Column> ParseColumns<T>(Expression<ColumnExpression<T>> expression, bool allowSubMembers = true)
        {
            var processor = new ExpressionProcessor(allowSubMembers);
            return processor.ParseColumns(info, expression);
        }
    }
}
