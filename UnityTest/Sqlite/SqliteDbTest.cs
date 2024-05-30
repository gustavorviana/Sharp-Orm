using SharpOrm.Builder;
using System.Data.SQLite;
using UnityTest.BaseTests;

namespace UnityTest.Sqlite
{
    public class SqliteDbTest : DbTableTest<SQLiteConnection>
    {
        public SqliteDbTest() : base(new SqliteQueryConfig(false) { LoadForeign = true }, SqliteTest.ConnStr)
        {
        }
    }
}
