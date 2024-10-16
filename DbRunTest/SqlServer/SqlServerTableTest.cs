using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer
{
    public class SqlServerTableTest(ITestOutputHelper output, UnsafeDbFixture<SqlConnection> connection) : DbTableTest<SqlConnection>(output, connection)
    {
    }
}
