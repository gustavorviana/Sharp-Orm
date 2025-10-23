using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Interfaces;
using SharpOrm.Connection;

namespace DbRunTest.Sqlite
{
    [Collection("SQLite")]
    public class QueryConfigTests(SqliteFixture connection) :
        DbTestBase(connection), IQueryTests
    {
        [Fact]
        public void GetVersionString()
        {
            var version = new Version("3.46.1");
            using var connection = Manager.Connection.OpenIfNeeded();

            Assert.Equal(version, connection.GetVersion());
        }
    }
}
