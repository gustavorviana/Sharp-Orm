using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite
{
    [Collection("SQLite")]
    public class SqliteBlob(ITestOutputHelper output, SqliteFixture connection) : DbBlobTest(output, connection)
    {
    }
}
