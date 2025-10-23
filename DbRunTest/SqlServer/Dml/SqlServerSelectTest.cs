using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    [Collection("SQL Server")]
    public class SqlServerSelectTest(ITestOutputHelper output, SqlServerFixture connection) : SelectTest(output, connection)
    {
    }
}
