using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Interfaces;
using SharpOrm.Connection;

namespace DbRunTest.MySql
{
    [Collection("MySQL")]
    public class QueryConfigTests(MysqlFixture connection) :
        DbTestBase(connection), IQueryTests
    {
        [Fact]
        public void GetVersionString()
        {
            var version = new Version("8.0");
            using var connection = Manager.Connection.OpenIfNeeded();

            Assert.True(version <= connection.GetVersion());
        }
    }
}
