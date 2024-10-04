using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using SharpOrm.Builder;
using System.Data.SqlClient;

namespace DbRunTest
{
    internal static class ConnectionMap
    {
        private const string FilePath = "../TestDatabase.sqlite";
        public const string SqliteConnStr = "Data Source=" + FilePath + ";Version=3;";

        private static readonly Dictionary<Type, ConnMapInfo> map = new()
        {
            { typeof(SqlConnection), new ConnMapInfo(new SqlServerQueryConfig(), GetFromFile("../SqlServerConnection.txt", @"Data Source=localhost;Initial Catalog=SharpOrm;Integrated Security=True;")) },
            { typeof(MySqlConnection), new ConnMapInfo(new MysqlQueryConfig(), GetFromFile("../MysqlConnection.txt", "Persist Security Info=False;server=localhost;database=SharpOrm;uid=root;pwd=root")) },
            { typeof(SqliteConnection), new ConnMapInfo(new SqliteQueryConfig(), SqliteConnStr) }
        };

        public static ConnMapInfo Get(Type dbType)
        {
            var config = map[dbType];

            if (string.IsNullOrEmpty(config.ConnString))
                throw new Exception("The Connection.txt file must contain the connection string.");

            return config;
        }

        public static string GetFromFile(string filePath, string connectionString)
        {
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, connectionString);

            return File.ReadAllText(filePath);
        }

        public record ConnMapInfo(QueryConfig Config, string ConnString);
    }
}
