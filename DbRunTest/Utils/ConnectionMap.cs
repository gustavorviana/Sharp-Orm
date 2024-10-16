using BaseTest.Mock;
using DbRunTest.DbInitializer;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data.SqlClient;

namespace DbRunTest.Utils
{
    internal static class ConnectionMap
    {
        private const string FilePath = "../TestDatabase.sqlite";
        public const string SqliteConnStr = "Data Source=" + FilePath;

        private static readonly Dictionary<Type, ConnMapInfo> map = new()
        {
            { typeof(MockConnection), new ConnMapInfo(typeof(SqlServerQueryConfig), null, "") },
            { typeof(SqlConnection), new ConnMapInfo(typeof(SqlServerQueryConfig), new SqlServerInicializer(), GetFromFile("../SqlServerConnection.txt", @"Data Source=localhost;Initial Catalog=SharpOrm;Integrated Security=True;")) },
            { typeof(MySqlConnection), new ConnMapInfo(typeof(MysqlQueryConfig) , new MysqlInicializer(), GetFromFile("../MysqlConnection.txt", "Persist Security Info=False;server=localhost;database=SharpOrm;uid=root;pwd=root")) },
            { typeof(SqliteConnection), new ConnMapInfo(typeof(SqliteQueryConfig), new SqliteInicializer(), SqliteConnStr) }
        };

        static ConnectionMap()
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
        }

        public static ConnMapInfo Get(Type dbType)
        {
            var config = map[dbType];

            if (dbType == typeof(MockConnection)) return config;

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

        public class ConnMapInfo(Type QueryConfigType, DbInicializer? inicializer, string connString)
        {
            private readonly Dictionary<bool, QueryConfig> config = [];
            public DbInicializer? Inicializer => inicializer;
            public string ConnString => connString;

            public QueryConfig GetConfig(bool safe = true)
            {
                if (config.TryGetValue(safe, out var queryConfig))
                    return queryConfig;

                return config[safe] = (QueryConfig)Activator.CreateInstance(QueryConfigType, safe ? null : new object[] { false })!;
            }
        }
    }
}
