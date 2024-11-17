using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    public class SqliteBulkOperationTest(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : BulkOperationTest<SqliteConnection>(output, connection)
    {
        [Fact]
        public override void DeleteTest()
        {
            Assert.Throws<NotSupportedException>(base.DeleteTest);
        }

        [Fact]
        public override void UpdateTest()
        {
            Assert.Throws<NotSupportedException>(base.UpdateTest);
        }
    }
}
