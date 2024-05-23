using System;
using System.Collections.Generic;
using System.IO;

namespace UnityTest.Utils
{
    public static class ConnectionStr
    {
        private static readonly Dictionary<string, string> cached = new();

        public static string Mysql => GetFromFile("../MysqlConnection.txt", "Persist Security Info=False;server=localhost;database=SharpOrm;uid=root;pwd=root");
        public static string SqlServer => GetFromFile("../SqlServerConnection.txt", @"Data Source=localhost;Initial Catalog=SharpOrm;Integrated Security=True;");

        public static string GetFromFile(string filePath, string connectionString)
        {
            if (cached.TryGetValue(filePath, out var result))
                return result;

            if (!File.Exists(filePath))
                File.WriteAllText(filePath, connectionString);

            var connString = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(connString))
                throw new Exception("The Connection.txt file must contain the connection string.");

            cached[filePath] = connString;

            return connString;
        }
    }
}