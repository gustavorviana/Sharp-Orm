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

        internal ExpressionProcessor<T> GetProcessor<T>(bool allowSubMembers)
        {
            return new ExpressionProcessor<T>(info, allowSubMembers);
        }

        protected IEnumerable<Column> ParseColumns<T>(Expression<ColumnExpression<T>> expression, bool allowSubMembers = true)
        {
            return this.GetProcessor<T>(allowSubMembers).ParseColumns(expression);
        }
    }
}
