using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlBulkOperationTest(ITestOutputHelper output, MysqlFixture connection) : BulkOperationTest(output, connection)
    {
    }
}
