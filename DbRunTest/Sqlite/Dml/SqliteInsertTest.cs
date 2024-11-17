using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    public class SqliteInsertTest(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : InsertTest<SqliteConnection>(output, connection)
    {
    }
}
