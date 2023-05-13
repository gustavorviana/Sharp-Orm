using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace UnityTest.Utils
{
    public class SqlServerTest : BaseTest
    {
        protected static readonly SqlServerQueryConfig NewConfig = new(false) { UseOldPagination = false };
        protected static readonly SqlServerQueryConfig OldConfig = new(false) { UseOldPagination = true };
        protected static DbConnection Connection
        {
            get
            {
                ConnectionStr.Boot<SqlConnection>(() => NewConfig, ConnectionStr.SqlServer);
                return ConnectionCreator.Default.GetConnection();
            }
        }

        #region Consts
        protected const string TABLE = "TestTable";

        protected const string ID = "id";
        protected const string NAME = "name";
        protected const string NICK = "nick";
        protected const string CREATEDAT = "record_created";
        protected const string NUMBER = "number";
        protected const string GUIDID = "custom_id";
        protected const string STATUS = "custom_status";
        #endregion


        #region Class Init/Clean

        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void InitConnection(TestContext context)
        {
            using var q = NewQuery("sysobjects");
            q.Where("NAME", TABLE).Where("xtype", "U");
            if (q.Count() > 0)
                return;

            using var conn = Connection;
            using var cmd = conn.CreateCommand();
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

        protected static Query NewQuery(string table, string alias = "", bool useNewConfig = true)
        {
            return new Query(Connection, useNewConfig ? NewConfig : OldConfig, new DbName(table, alias));
        }

        protected static Row NewRow(int id, string name)
        {
            return new Row(
                new Cell(ID, id),
                new Cell(NAME, name),
                new Cell(NUMBER, id),
                new Cell(GUIDID, Guid.NewGuid().ToString()),
                new Cell(STATUS, 1)
            );
        }

        private static string GetCreateTableSql()
        {
            return @$"CREATE TABLE [{TABLE}] (
                      [{ID}] INT NOT NULL PRIMARY KEY,
                      [{NAME}] VARCHAR(256) NOT NULL,
                      [{NICK}] VARCHAR(256) NULL,
                      [{CREATEDAT}] DATETIME DEFAULT GETDATE(),
                      [{NUMBER}] DECIMAL(13,2) NOT NULL,
                      [{GUIDID}] VARCHAR(36) NOT NULL,
                      [{STATUS}] INT NOT NULL
                );";
        }

        [TestCleanup]
        [TestInitialize]
        public void CleanupTest()
        {
            if (!this.TestContext.Properties.Contains("clearDb"))
                return;

            using var query = NewQuery(TABLE);
            query.Delete();
        }

        protected void InsertRows(int count)
        {
            try
            {
                Row[] rows = new Row[count];

                for (int i = 1; i <= count; i++)
                    rows[i - 1] = NewRow(i, $"User {i}");

                using var q = NewQuery(TABLE);
                q.BulkInsert(rows);
            }
            catch (System.Exception)
            {
            }
        }
    }
}
