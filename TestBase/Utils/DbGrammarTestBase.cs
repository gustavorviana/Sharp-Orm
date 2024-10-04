using SharpOrm.Builder;
using BaseTest.Fixtures;
using Xunit.Abstractions;
using SharpOrm;

namespace BaseTest.Utils
{
    public class DbGrammarTestBase(ITestOutputHelper output, DbFixtureBase connection) : DbTestBase(output, connection)
    {
        protected Grammar GetGrammar(Query query)
        {
            return this.Config.NewGrammar(query);
        }

        protected TableGrammar GetTableGrammar(TableSchema schema)
        {
            return this.Config.NewTableGrammar(schema);
        }
    }
}
