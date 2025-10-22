using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.MySql
{
    [Collection("MySQL Unsafe")]
    public class MySqlTableTest(ITestOutputHelper output, UnsafeDbFixture<MySqlConnection> connection) : DbTableTest(output, connection)
    {
    }
}
