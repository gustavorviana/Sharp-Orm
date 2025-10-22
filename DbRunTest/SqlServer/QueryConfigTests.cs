using BaseTest.Fixtures;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Interfaces;
using SharpOrm.Connection;
using System.Data.SqlClient;

namespace DbRunTest.SqlServer
{
    [Collection("SQL Server")]
    public class QueryConfigTests(DbFixture<SqlConnection> connection) :
        DbTestBase(connection), IQueryTests
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
