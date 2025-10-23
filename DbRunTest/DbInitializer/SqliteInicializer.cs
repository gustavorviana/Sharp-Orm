using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    internal class SqliteInicializer : DbInicializer
    {
        public override async Task InitAsync(ConnectionManager manager)
        {
            var script = File.ReadAllText("./Scripts/Sqlite.sql");
            var commands = script.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var command in commands)
            {
                var trimmedCommand = command.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedCommand))
                {
                    await manager.ExecuteNonQueryAsync(trimmedCommand);
                }
            }
        }

        public override Task ResetAsync(ConnectionManager manager)
        {
            return Task.CompletedTask;
        }
    }
}
