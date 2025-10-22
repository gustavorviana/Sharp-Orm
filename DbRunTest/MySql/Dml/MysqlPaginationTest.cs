using BaseTest.Fixtures;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlPaginationTest(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : PaginationTest(output, connection)
    {
    }
}
