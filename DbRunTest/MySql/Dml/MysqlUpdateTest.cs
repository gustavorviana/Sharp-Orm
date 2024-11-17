using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    public class MysqlUpdateTest(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : UpdateTest<MySqlConnection>(output, connection)
    {
    }
}
