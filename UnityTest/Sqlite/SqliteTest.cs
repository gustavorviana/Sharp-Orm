using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data.SQLite;
using UnityTest.Utils;

namespace UnityTest.Sqlite
{
    public class SqliteTest : DbTest<SQLiteConnection>
    {
        private const string ConnStr = "Data Source=MyDatabase.sqlite;Version=3;";
        public SqliteTest() : base(new SqliteQueryConfig(false) { LoadForeign = true }, ConnStr)
        {

        }

        protected static ConnectionCreator GetCreator()
        {
            return new SingleConnectionCreator<SQLiteConnection>(new SqliteQueryConfig(), ConnStr);
        }
    }
}
