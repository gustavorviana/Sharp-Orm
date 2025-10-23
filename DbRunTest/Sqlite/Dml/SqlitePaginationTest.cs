using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    [Collection("SQLite")]
    public class SqlitePaginationTest(ITestOutputHelper output, SqliteFixture connection) : PaginationTest(output, connection)
    {
    }
}
