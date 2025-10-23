using BaseTest.Fixtures;
using BaseTest.Utils;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;
using Xunit.Abstractions;

namespace QueryTest.Utils
{
    public class DbGrammarTestBase : QueryTestBase
    {
        public DbGrammarTestBase(ITestOutputHelper output, DbFixtureBase connection) : base(output, connection)
        {

        }

        public DbGrammarTestBase(ITestOutputHelper output, QueryConfig config) : base(output, MockFixture.FromConfig(config))
        {

        }

        protected TableGrammar GetTableGrammar(ITableSchema schema)
        {
            return Config.NewTableGrammar(schema);
        }
    }
}
