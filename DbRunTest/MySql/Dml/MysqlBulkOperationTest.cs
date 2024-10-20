using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    public class MysqlBulkOperationTest(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : BulkOperationTest<MySqlConnection>(output, connection)
    {
    }
}
