using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;

namespace UnityTest.Utils
{
    public class SqlServerTest : BaseTest
    {
        protected static SqlServerQueryConfig config = new SqlServerQueryConfig(false);
        protected static DbConnection connection;

        #region Consts
        protected const string TABLE = "TestTable";

        protected const string ID = "id";
        protected const string NAME = "name";
        protected const string NICK = "nick";
        protected const string CREATEDAT = "record_created";
        #endregion

        #region Class Init/Clean

        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void InitConnection(TestContext context)
        {
            ReloadConnection();

            using var q = NewQuery("sysobjects");
            q.Where("NAME", TABLE).Where("xtype", "U");
            if (q.Count() > 0)
                return;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = GetCreateTableSql();
            cmd.ExecuteNonQuery();
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

        protected static void ReloadConnection()
        {
            CloseConnection();
            connection = new SqlConnection(GetConnectionString());
            connection.Open();

            QueryDefaults.Config = config;
            QueryDefaults.Connection = connection;
        }

        private static void CloseConnection()
        {
            try
            {
                if (connection != null)
                    connection.Dispose();

                connection = null;
            }
            catch
            {
            }
        }

        private static string GetConnectionString()
        {
            string file = "SqlServerConnection.txt";
            if (!File.Exists(file))
                File.WriteAllText(file, @"Data Source=localhost\SQLEXPRESS;Initial Catalog=SharpOrm;Integrated Security=True;");

            var connString = File.ReadAllText(file);

            if (string.IsNullOrEmpty(connString))
                throw new Exception($"O arquivo {file} deve conter a string de conexão.");

            return connString;
        }

        protected static Query NewQuery(string table, string alias = "")
        {
            return new Query(connection, config, table, alias);
        }

        protected static Row NewRow(int id, string name)
        {
            return new Row(new Cell[] { new Cell(ID, id), new Cell(NAME, name) });
        }

        private static string GetCreateTableSql()
        {
            return @$"CREATE TABLE [{TABLE}] (
                      [{ID}] INT NOT NULL PRIMARY KEY,
                      [{NAME}] VARCHAR(256) NOT NULL,
                      [{NICK}] VARCHAR(256) NULL,
                      [{CREATEDAT}] DATETIME DEFAULT GETDATE()
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
    }
}
