using MySql.Data.MySqlClient;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace UnityTest.Utils
{
    public abstract class MysqlTest : DbTest<MySqlConnection>
    {
        public MysqlTest() : base(new MysqlQueryConfig(false) { LoadForeign = true }, ConnectionStr.Mysql)
        {

        }

        protected static ConnectionCreator GetCreator()
        {
            return new SingleConnectionCreator<MySqlConnection>(new MysqlQueryConfig(), ConnectionStr.Mysql);
        }
    }
}
