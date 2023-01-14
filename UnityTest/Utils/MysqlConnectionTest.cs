using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.IO;

namespace UnityTest.Utils
{
    public abstract class MysqlConnectionTest : BaseTest
    {
        protected static MySqlConnection Connection
        {
            get
            {
                if (ConnectionCreator.Default is not ConnectionCreator<MySqlConnection>)
                    ConnectionCreator.Default = new ConnectionCreator<MySqlConnection>(new MysqlQueryConfig(false), GetConnectionString());

                return (MySqlConnection)ConnectionCreator.Default.OpenConnection();
            }
        }

        private static string GetConnectionString()
        {
            string file = "Connection.txt";
            if (!File.Exists(file))
                File.WriteAllText(file, "Persist Security Info=False;server=localhost;database=SharpOrm;uid=root;server=localhost;uid=root;pwd=root");

            var connString = File.ReadAllText(file);

            if (string.IsNullOrEmpty(connString))
                throw new Exception("O arquivo Connection.txt deve conter a string de conexão.");

            return connString;
        }

        protected static Query NewQuery(string table, string alias = "")
        {
            return new Query(Connection, table, alias);
        }
    }
}
