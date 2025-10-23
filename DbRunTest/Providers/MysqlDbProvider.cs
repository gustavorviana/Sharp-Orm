using DbRunTest.DbInitializer;
using MySql.Data.MySqlClient;
using SharpOrm.Builder;
using Testcontainers.MySql;

namespace DbRunTest.Providers
{
    internal class MysqlDbProvider : DbProvider<MysqlQueryConfig, MySqlConnection>
    {
        private static MySqlContainer? _mySqlContainer;

        protected override DbInicializer? Initializer { get; } = new MysqlInicializer();

        protected override async Task<string> OnBuildAsync()
        {
            _mySqlContainer = new MySqlBuilder()
                    .WithCommand("--lower_case_table_names=1")
                    .WithDatabase("SharpOrm")
                    .WithUsername("root")
                    .WithPassword("root")
                    .Build();

            await _mySqlContainer.StartAsync();

            return _mySqlContainer.GetConnectionString();
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            if (disposing && _mySqlContainer != null)
                await _mySqlContainer.DisposeAsync();
        }
    }
}
