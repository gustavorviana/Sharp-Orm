using BaseTest.Fixtures;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    [Collection("SQL Server")]
    public class SqlServerBulkOperationTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : BulkOperationTest(output, connection)
    {
    }
}
