using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;

namespace UnityTest.Utils
{
    public class MysqlTableTest : MysqlConnectionTest
    {
        private static bool hasFirstLoad = false;
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
            using var cmd = connection.CreateCommand();
            cmd.CommandText = GetCreateTableSql();
            cmd.ExecuteNonQuery();

            if (hasFirstLoad)
                return;

            hasFirstLoad = true;

            try
            {
                var QueryConfig = new MysqlQueryConfig();
                //Utilizado para carregar as bibliotecas para reduzir o tempo de execução "falso" do código.

                using var q = NewQuery();
                q.Where("column", "=", "value");

                using var g = QueryConfig.NewGrammar(q);
                using (_ = g.GetSelectCommand()) { }
            }
            catch
            { }
        }

        [ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void CleanupDbConnection()
        {
            ReloadConnection();
            using var con = connection;
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

        [TestInitialize]
        public void ResetDefaultsOnTestInitialize()
        {
            QueryDefaults.Config = new MysqlQueryConfig();
            QueryDefaults.Connection = null;
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
