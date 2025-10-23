using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Interfaces;
using SharpOrm.Connection;

namespace DbRunTest.SqlServer
{
    [Collection("SQL Server")]
    public class QueryConfigTests(SqlServerFixture connection) :
        DbTestBase(connection), IQueryTests
    {
        [Fact]
        public void GetVersionString()
        {
            var version = new Version("15.0.4261");
            using var connection = Manager.Connection.OpenIfNeeded();

            Assert.Equal(version, connection.GetVersion());
        }
    }
}
