using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Interfaces;
using MySql.Data.MySqlClient;
using SharpOrm.Connection;

namespace DbRunTest.MySql
{
    public class QueryConfigTests(DbFixture<MySqlConnection> connection) :
        DbTestBase(connection), IClassFixture<DbFixture<MySqlConnection>>, IQueryTests
    {
        [Fact]
        public void GetVersionVersionString()
        {
            var version = new Version("8.0");
            using var connection = Manager.Connection.OpenIfNeeded();

            Assert.True(version <= connection.GetVersion());
        }
    }
}
