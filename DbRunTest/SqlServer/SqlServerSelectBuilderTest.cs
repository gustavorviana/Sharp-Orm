using BaseTest.Utils;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class SqlServerSelectBuilderTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : DbTestBase(output, connection), IClassFixture<DbFixture<SqlConnection>>
    {
        [Fact]
        public void Test()
        {

        }
    }
}
