using BaseTest.Utils;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer
{
    [Collection("SQL Server")]
    public class SqlServerSelectBuilderTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : DbTestBase(output, connection)
    {
        [Fact]
        public void Test()
        {

        }
    }
}
