using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    [Collection("SQL Server")]
    public class SqlServerDeleteTest(ITestOutputHelper output, SqlServerFixture connection) : DeleteTest(output, connection)
    {
    }
}
