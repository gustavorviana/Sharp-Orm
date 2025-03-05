using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql
{
    public class MySqlBlob(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : DbBlobTest<MySqlConnection>(output, connection)
    {
    }
}
