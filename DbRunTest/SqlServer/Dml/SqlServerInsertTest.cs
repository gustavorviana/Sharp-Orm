using BaseTest.Utils;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    [Collection("SQL Server")]
    public class SqlServerInsertTest(ITestOutputHelper output, SqlServerFixture connection) : InsertTest(output, connection)
    {
        public override void Insert()
        {
            using var q = NewQuery(GetUnsafeManager(), TestTableUtils.TABLE);
            q.Insert(TestTableUtils.NewRow(120, "User 1").Cells);

            Assert.Equal(1, q.Count());
        }
    }
}
