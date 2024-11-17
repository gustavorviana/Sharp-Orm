using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    public class MysqlInsertTest(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : InsertTest<MySqlConnection>(output, connection)
    {
    }
}
