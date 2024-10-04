using BaseTest.Utils;
using QueryTest.Fixtures;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class SqlServerSelectBuilderTest(ITestOutputHelper output, DbFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<DbFixture<SqlServerQueryConfig>>
    {
        [Fact]
        public void Test()
        {

        }
    }
}
