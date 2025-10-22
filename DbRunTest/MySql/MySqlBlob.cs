using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql
{
    [Collection("MySQL")]
    public class MySqlBlob(ITestOutputHelper output, DbFixture<MySqlConnection> connection) : DbBlobTest(output, connection)
    {
    }
}
