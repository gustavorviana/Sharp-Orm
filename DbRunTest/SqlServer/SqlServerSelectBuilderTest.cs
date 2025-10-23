using BaseTest.Utils;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer
{
    [Collection("SQL Server")]
    public class SqlServerSelectBuilderTest(ITestOutputHelper output, SqlServerFixture connection) : DbTestBase(output, connection)
    {
        [Fact]
        public void Test()
        {

        }
    }
}
