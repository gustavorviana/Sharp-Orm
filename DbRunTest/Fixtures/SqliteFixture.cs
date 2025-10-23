using DbRunTest.Providers;
using Microsoft.Data.Sqlite;

namespace DbRunTest.Fixtures
{
    public class SqliteFixture : DbFixture<SqliteConnection>
    {
        static SqliteFixture()
        {
            DbProvider.Register(new SqliteDbProvider());
        }
    }
}
