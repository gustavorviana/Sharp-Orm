using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlUpdateTest(ITestOutputHelper output, MysqlFixture connection) : UpdateTest(output, connection)
    {
    }
}
