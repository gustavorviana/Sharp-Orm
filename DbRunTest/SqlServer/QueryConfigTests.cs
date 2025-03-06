using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Interfaces;
using SharpOrm.Connection;
using System.Data.SqlClient;

namespace DbRunTest.SqlServer
{
    public class QueryConfigTests(DbFixture<SqlConnection> connection) :
        DbTestBase(connection), IClassFixture<DbFixture<SqlConnection>>, IQueryTests
    {
        [Fact]
        public void GetVersionVersionString()
        {
            var version = new Version("16.0");
            using var connection = Manager.Connection.OpenIfNeeded();

            Assert.True(version <= connection.GetVersion());
        }
    }
}
