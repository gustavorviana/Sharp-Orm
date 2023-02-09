using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using System.Data.Common;
using System.IO;

namespace UnityTest.Utils
{
    public class SqlServerTest : BaseTest
    {
        protected static readonly SqlServerQueryConfig config = new(false);
        protected static DbConnection Connection
        {
            get
            {
                if (ConnectionCreator.Default is not SingleConnectionCreator)
                    ConnectionCreator.Default = new SingleConnectionCreator(new SqlServerQueryConfig(false), GetConnectionString());

                return ConnectionCreator.Default.GetConnection();
            }
        }

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
            return new Query(Connection, config, table, alias);
        }

        protected static Row NewRow(int id, string name)
        {
            return new Row(new Cell(ID, id), new Cell(NAME, name));
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
