using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql
{
    [Collection("MySQL")]
    public class MySqlBlob(ITestOutputHelper output, MysqlFixture connection) : DbBlobTest(output, connection)
    {
    }
}
