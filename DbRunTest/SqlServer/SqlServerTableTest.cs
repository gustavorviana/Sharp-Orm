using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer
{
    [Collection("SQL Server Unsafe")]
    public class SqlServerTableTest(ITestOutputHelper output, UnsafeDbFixture<SqlConnection> connection) : DbTableTest(output, connection)
    {
    }
}
