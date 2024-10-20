using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    public class SqlServerDeleteTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : DeleteTest<SqlConnection>(output, connection)
    {
    }
}
