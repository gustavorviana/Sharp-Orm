using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlPaginationTest(ITestOutputHelper output, MysqlFixture connection) : PaginationTest(output, connection)
    {
    }
}
