using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlDeleteTest(ITestOutputHelper output, MysqlFixture connection) : DeleteTest(output, connection)
    {
    }
}
