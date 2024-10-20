using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite
{
    public class SqliteTableTest(ITestOutputHelper output, UnsafeDbFixture<SqliteConnection> connection) : DbTableTest<SqliteConnection>(output, connection)
    {
    }
}
