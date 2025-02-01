using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;

namespace QueryTest.Utils
{
    public static class QueryExtensions
    {
        public static Grammar Grammar(this Query query)
        {
            return query.Config.NewGrammar(query);
        }

        public static string ToString(this ISqlExpressible sqlExpressible, Query query, bool allowAlias = false)
        {
            return ToString(sqlExpressible, query.Info, allowAlias);
        }

        public static string ToString(this SqlExpression expression, IReadonlyQueryInfo info, bool allowAlias = false)
        {
            return LoadDeferred(expression, info, allowAlias).ToString();
        }

        public static string ToString(this ISqlExpressible sqlExpressible, IReadonlyQueryInfo info, bool allowAlias = false)
        {
            return LoadDeferred(sqlExpressible.ToSafeExpression(info, allowAlias), info, allowAlias).ToString();
        }

        public static SqlExpression LoadDeferred(this Column expression, IReadonlyQueryInfo info, bool allowAlias = false)
        {
            return new QueryBuilder(info).AddParameter(expression, allowAlias).ToExpression(true);
        }

        public static SqlExpression LoadDeferred(this SqlExpression expression, IReadonlyQueryInfo info, bool allowAlias = false)
        {
            return new QueryBuilder(info).AddParameter(expression, allowAlias).ToExpression(true);
        }
    }
}
