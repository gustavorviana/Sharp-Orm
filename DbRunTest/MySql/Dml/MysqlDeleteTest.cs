using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    public class MysqlDeleteTest(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : DeleteTest<MySqlConnection>(output, connection)
    {
    }
}
