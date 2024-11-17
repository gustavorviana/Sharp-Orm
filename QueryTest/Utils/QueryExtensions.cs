using SharpOrm;
using SharpOrm.Builder;

namespace QueryTest.Utils
{
    public static class QueryExtensions
    {
        public static Grammar Grammar(this Query query)
        {
            return query.Config.NewGrammar(query);
        }
    }
}
