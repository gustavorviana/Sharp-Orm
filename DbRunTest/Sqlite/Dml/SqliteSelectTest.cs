using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    [Collection("SQLite")]
    public class SqliteSelectTest(ITestOutputHelper output, SqliteFixture connection) : SelectTest(output, connection)
    {
    }
}
