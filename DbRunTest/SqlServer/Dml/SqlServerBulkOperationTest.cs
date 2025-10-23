using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    [Collection("SQL Server")]
    public class SqlServerBulkOperationTest(ITestOutputHelper output, SqlServerFixture connection) : BulkOperationTest(output, connection)
    {
    }
}
