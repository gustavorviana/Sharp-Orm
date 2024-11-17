using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    public class MysqlSelectTest(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : SelectTest<MySqlConnection>(output, connection)
    {
    }
}
