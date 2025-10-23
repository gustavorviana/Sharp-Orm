using DbRunTest.DbInitializer;
using SharpOrm.Builder;
using System.Data.SqlClient;
using Testcontainers.MsSql;

namespace DbRunTest.Providers
{
    internal class SqlServerDbProvider : DbProvider<SqlServerQueryConfig, SqlConnection>
    {
        private static MsSqlContainer? _sqlServerContainer;

        protected override DbInicializer? Initializer { get; } = new SqlServerInicializer();

        protected override async Task<string> OnBuildAsync()
        {
            _sqlServerContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
            .Build();

            await _sqlServerContainer.StartAsync();

            return _sqlServerContainer.GetConnectionString();
        }
    }
}
