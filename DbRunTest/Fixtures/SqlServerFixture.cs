using DbRunTest.Providers;
using System.Data.SqlClient;

namespace DbRunTest.Fixtures
{
    public class SqlServerFixture : DbFixture<SqlConnection>
    {
        static SqlServerFixture()
        {
            DbProvider.Register(new SqlServerDbProvider());
        }
    }
}
