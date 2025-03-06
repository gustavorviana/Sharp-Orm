using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Interfaces;
using Microsoft.Data.Sqlite;
using SharpOrm.Connection;

namespace DbRunTest.Sqlite
{
    public class QueryConfigTests(DbFixture<SqliteConnection> connection) :
        DbTestBase(connection), IClassFixture<DbFixture<SqliteConnection>>, IQueryTests
    {
        [Fact]
        public void GetVersionVersionString()
        {
            var version = new Version("3.46.1");
            using var connection = Manager.Connection.OpenIfNeeded();

            Assert.Equal(version, connection.GetVersion());
        }
    }
}
