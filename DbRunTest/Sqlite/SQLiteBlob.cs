using BaseTest.Fixtures;
using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite
{
    [Collection("SQLite")]
    public class SqliteBlob(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : DbBlobTest(output, connection)
    {
    }
}
