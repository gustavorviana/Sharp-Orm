using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;

namespace UnityTest.Utils
{
    public class MysqlTableTest : MysqlConnectionTest
    {
        #region Consts
        protected const string TABLE = "TestTable";

        protected const string ID = "id";
        protected const string NAME = "name";
        protected const string NICK = "nick";
        protected const string CREATEDAT = "record_created";
        #endregion

        #region Class Init/Clean
        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void OnMysqlTableTestInit(TestContext context)
        {
            using var con = Connection;
            using var cmd = con.CreateCommand();
            cmd.CommandText = GetCreateTableSql();
            cmd.ExecuteNonQuery();
        }

        [ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void CleanupDbConnection()
        {
            using var con = Connection;
            using var cmd = con.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS {TABLE}";
            cmd.ExecuteNonQuery();
        }
        #endregion

        protected static Row NewRow(int id, string name)
        {
            return new Row(new Cell[] { new Cell(ID, id), new Cell(NAME, name) });
        }

        protected static void InsertRows(int count)
        {
            Row[] rows = new Row[count];

            for (int i = 1; i <= count; i++)
                rows[i - 1] = NewRow(i, $"User {i}");

            using var q = NewQuery();
            q.BulkInsert(rows);
        }

        protected static Query NewQuery()
        {
            return NewQuery(TABLE);
        }

        private static string GetCreateTableSql()
        {
            return $@"CREATE TABLE IF NOT EXISTS {TABLE} (
                  {ID} INT NOT NULL PRIMARY KEY,
                  {NAME} VARCHAR(256) NOT NULL,
                  {NICK} VARCHAR(256) NULL,
                  {CREATEDAT} TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
        }
    }
}
