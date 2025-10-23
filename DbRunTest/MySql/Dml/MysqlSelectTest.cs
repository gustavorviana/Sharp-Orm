using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlSelectTest(ITestOutputHelper output, MysqlFixture connection) : SelectTest(output, connection)
    {
    }
}
