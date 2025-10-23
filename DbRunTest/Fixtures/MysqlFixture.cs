using DbRunTest.Providers;
using MySql.Data.MySqlClient;

namespace DbRunTest.Fixtures
{
    public class MysqlFixture : DbFixture<MySqlConnection>
    {
        static MysqlFixture()
        {
            DbProvider.Register(new MysqlDbProvider());
        }
    }
}
