using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using SharpOrm;
using System;
using System.IO;

namespace UnityTest.Utils
{
    public abstract class MysqlConnectionTest : BaseTest
    {
        protected static MySqlConnection connection;

        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void InitConnection(TestContext context)
        {
            ReloadConnection();
        }

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

        protected static Query NewQuery(string table, string alias = "")
        {
            return new Query(connection, table, alias);
        }
    }
}
