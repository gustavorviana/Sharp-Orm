using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql
{
    [Collection("MySQL")]
    public class MySqlTableTest : DbTableTest
    {
        public MySqlTableTest(ITestOutputHelper output, MysqlFixture connection) : base(output, connection)
        {
            MakeUnsafe();
        }
    }
}
