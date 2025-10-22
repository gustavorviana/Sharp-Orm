using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    [Collection("SQLite Unsafe")]
    public class SqliteBulkOperationTest(ITestOutputHelper output, UnsafeDbFixture<SqliteConnection> connection) : BulkOperationTest(output, connection)
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
