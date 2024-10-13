using SharpOrm.Builder;
using BaseTest.Fixtures;
using Xunit.Abstractions;
using QueryTest.Fixtures;
using BaseTest.Utils;

namespace QueryTest.Utils
{
    public class DbGrammarTestBase : QueryTestBase
    {
        public DbGrammarTestBase(ITestOutputHelper output, DbFixtureBase connection) : base(output, connection)
        {

        }

        public DbGrammarTestBase(ITestOutputHelper output, QueryConfig config) : base(output, new DbFixture(config))
        {

        }

        public DbGrammarTestBase(QueryConfig config) : base(null, new DbFixture(config))
        {

        }

        protected TableGrammar GetTableGrammar(TableSchema schema)
        {
            return this.Config.NewTableGrammar(schema);
        }
    }
}
