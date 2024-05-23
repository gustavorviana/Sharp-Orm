using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data.SQLite;
using System.IO;
using UnityTest.Utils;

namespace UnityTest.Sqlite
{
    public class SqliteTest : DbTest<SQLiteConnection>
    {
        private const string FilePath = "../TestDatabase.sqlite";
        private const string ConnStr = "Data Source=" + FilePath + ";Version=3;";
        public SqliteTest() : base(new SqliteQueryConfig(false) { LoadForeign = true }, ConnStr)
        {
            if (!File.Exists(FilePath))
                File.Create(FilePath).Dispose();
        }

        protected static ConnectionCreator GetCreator()
        {
            return new SingleConnectionCreator<SQLiteConnection>(new SqliteQueryConfig(), ConnStr);
        }
    }
}
