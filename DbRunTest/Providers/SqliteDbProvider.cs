using DbRunTest.DbInitializer;
using Microsoft.Data.Sqlite;
using SharpOrm.Builder;

namespace DbRunTest.Providers
{
    internal class SqliteDbProvider : DbProvider<SqliteQueryConfig, SqliteConnection>
    {
        private const string FilePath = "../TestDatabase.sqlite";
        public const string SqliteConnStr = "Data Source=" + FilePath;

        protected override DbInicializer? Initializer { get; } = new SqliteInicializer();

        static SqliteDbProvider()
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
        }

        protected override async Task<string> OnBuildAsync()
        {
            await Task.CompletedTask;
            return SqliteConnStr;
        }
    }
}
