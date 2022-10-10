using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.IO;

namespace Teste.Utils
{
    public class MysqlTableTest : BaseTest
    {
        private static bool hasFirstLoad = false;
        #region Consts
        protected const string TABLE = "TestTable";

        protected const string ID = "id";
        protected const string NAME = "name";
        protected const string NICK = "nick";
        protected const string CREATEDAT = "record_created";
        #endregion

        protected static MySqlConnection connection;

        #region Class Init/Clean
        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void InitConnection(TestContext context)
        {
            ReloadConnection();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = GetCreateTableSql();
            cmd.ExecuteNonQuery();

            if (hasFirstLoad)
                return;

            hasFirstLoad = true;

            try
            {
                var QueryConfig = new DefaultQueryConfig();
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

        protected static Query NewQuery()
        {
            return new Query(connection, TABLE);
        }

        [TestInitialize]
        public void ResetDefaultsOnTestInitialize()
        {
            QueryDefaults.Config = new DefaultQueryConfig();
            QueryDefaults.Connection = null;
        }

        #region Connection management
        protected static void ReloadConnection()
        {
            CloseConnection();
            connection = new MySqlConnection(GetConnectionString());
            connection.Open();
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
            string file = "Connection.txt";
            if (!File.Exists(file))
                File.WriteAllText(file, "Persist Security Info=False;server=localhost;database=Cadastro;uid=root;server=localhost;database=Cadastro;uid=root;pwd=root");

            var connString = File.ReadAllText(file);

            if (string.IsNullOrEmpty(connString))
                throw new Exception("O arquivo Connection.txt deve conter a string de conexão.");

            return connString;
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
        #endregion
    }
}
