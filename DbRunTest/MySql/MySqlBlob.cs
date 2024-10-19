using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.Mysql
{
    public class MySqlBlob(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : DbBlobTest<MySqlConnection>(output, connection)
    {
    }
}
