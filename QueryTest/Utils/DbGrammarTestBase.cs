using BaseTest.Fixtures;
using BaseTest.Utils;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Utils
{
    public class DbGrammarTestBase : QueryTestBase
    {
        public DbGrammarTestBase(ITestOutputHelper output, DbFixtureBase connection) : base(output, connection)
        {

        }

        public DbGrammarTestBase(ITestOutputHelper output, QueryConfig config) : base(output, new MockFixture(config))
        {

        }

        protected TableGrammar GetTableGrammar(TableSchema schema)
        {
            return this.Config.NewTableGrammar(schema);
        }
    }
}
