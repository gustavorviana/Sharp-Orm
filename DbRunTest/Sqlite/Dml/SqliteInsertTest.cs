using BaseTest.Fixtures;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    [Collection("SQLite")]
    public class SqliteInsertTest(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : InsertTest(output, connection)
    {
    }
}
