using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    public class SqlitePaginationTest(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : PaginationTest<SqliteConnection>(output, connection)
    {
    }
}
