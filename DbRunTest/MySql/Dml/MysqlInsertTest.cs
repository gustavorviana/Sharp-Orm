using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.MySql.Dml
{
    [Collection("MySQL")]
    public class MysqlInsertTest(ITestOutputHelper output, MysqlFixture connection) : InsertTest(output, connection)
    {
    }
}
