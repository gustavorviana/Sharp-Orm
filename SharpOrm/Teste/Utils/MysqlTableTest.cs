using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using System;
using System.IO;

namespace Teste.Utils
{
    public class MysqlTableTest : BaseTest
    {
        public const string TABLE = "TestTable";
        protected static MySqlConnection connection;

        #region Class Init/Clean
        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void InitConnection(TestContext context)
        {
            ReloadConnection();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = GetCreateTableSql();
            cmd.ExecuteNonQuery();
        }

        [ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void CleanupDbConnection()
        {
            using var con = connection;
            using var cmd = con.CreateCommand();
            cmd.CommandText = $"DROP TABLE {TABLE}";
            cmd.ExecuteNonQuery();
        }
        #endregion

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
                  id INT NOT NULL PRIMARY KEY,
                  name VARCHAR(256) NOT NULL,
                  record_created TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
        }
        #endregion
    }
}
