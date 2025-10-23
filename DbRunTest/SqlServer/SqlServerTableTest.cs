using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer
{
    [Collection("SQL Server")]
    public class SqlServerTableTest : DbTableTest
    {
        public SqlServerTableTest(ITestOutputHelper output, SqlServerFixture connection) : base(output, connection)
        {
            MakeUnsafe();
        }
    }
}
