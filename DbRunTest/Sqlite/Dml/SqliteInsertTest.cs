using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    [Collection("SQLite")]
    public class SqliteInsertTest(ITestOutputHelper output, SqliteFixture connection) : InsertTest(output, connection)
    {
    }
}
