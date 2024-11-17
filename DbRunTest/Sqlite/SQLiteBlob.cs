using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite
{
    public class SqliteBlob(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : DbBlobTest<SqliteConnection>(output, connection)
    {
    }
}
