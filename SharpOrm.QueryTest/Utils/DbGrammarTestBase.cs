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
    }
}
