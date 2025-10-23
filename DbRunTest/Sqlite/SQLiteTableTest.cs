using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite
{
    [Collection("SQLite")]
    public class SqliteTableTest : DbTableTest
    {
        public SqliteTableTest(ITestOutputHelper output, SqliteFixture connection)
            : base(output, connection)
        {
            MakeUnsafe();
        }
    }
}
