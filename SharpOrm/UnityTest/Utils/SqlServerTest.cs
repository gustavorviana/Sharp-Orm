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

        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void InitConnection(TestContext context)
        {
            ReloadConnection();
        }

        protected static void ReloadConnection()
        {
            CloseConnection();
            connection = new SqlConnection(GetConnectionString());
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
    }
}
