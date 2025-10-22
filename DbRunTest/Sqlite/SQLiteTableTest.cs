using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite
{
    [Collection("SQLite Unsafe")]
    public class SqliteTableTest(ITestOutputHelper output, UnsafeDbFixture<SqliteConnection> connection) : DbTableTest(output, connection)
    {
    }
}
