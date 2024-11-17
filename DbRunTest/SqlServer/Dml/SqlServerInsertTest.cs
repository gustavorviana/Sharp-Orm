using BaseTest.Utils;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    public class SqlServerInsertTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : InsertTest<SqlConnection>(output, connection)
    {
        public override void Insert()
        {
            using var q = NewQuery(this.GetUnsafeManager(), TestTableUtils.TABLE);
            q.Insert(TestTableUtils.NewRow(120, "User 1").Cells);

            Assert.Equal(1, q.Count());
        }
    }
}
