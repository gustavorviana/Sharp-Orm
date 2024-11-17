using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    public class SqlServerBulkOperationTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : BulkOperationTest<SqlConnection>(output, connection)
    {
    }
}
