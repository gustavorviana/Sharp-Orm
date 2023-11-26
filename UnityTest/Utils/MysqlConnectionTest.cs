using MySql.Data.MySqlClient;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace UnityTest.Utils
{
    public abstract class MysqlConnectionTest : BaseTest
    {
        protected static MySqlConnection Connection
        {
            get
            {
                ConnectionStr.Boot<MySqlConnection>(() => new MysqlQueryConfig(false) { LoadForeign = true }, ConnectionStr.Mysql);
                return (MySqlConnection)ConnectionCreator.Default.GetConnection();
            }
        }

        protected static Query NewQuery(string table, string alias = "")
        {
            return new Query(Connection, $"{table} {alias}");
        }

        protected static Query<T> NewQuery<T>(string alias = "") where T : new()
        {
            return new Query<T>(Connection, alias);
        }
    }
}
