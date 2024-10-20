using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    public class SqliteSelectTest(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : SelectTest<SqliteConnection>(output, connection)
    {
    }
}
