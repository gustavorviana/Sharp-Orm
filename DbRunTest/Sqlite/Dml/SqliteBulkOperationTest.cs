using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    [Collection("SQLite")]
    public class SqliteBulkOperationTest : BulkOperationTest
    {
        public SqliteBulkOperationTest(ITestOutputHelper output, SqliteFixture fixture) : base(output, fixture)
        {
            MakeUnsafe();
        }

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
