using BaseTest.Fixtures;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlDeleteTest(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : DeleteTest(output, connection)
    {
    }
}
