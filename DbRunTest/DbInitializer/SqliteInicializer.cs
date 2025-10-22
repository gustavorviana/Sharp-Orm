using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    internal class SqliteInicializer : DbInicializer
    {
        public override void InitDb(ConnectionManager manager)
        {
            var script = File.ReadAllText("./Scripts/Sqlite.sql");
            var commands = script.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var command in commands)
            {
                var trimmedCommand = command.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedCommand))
                {
                    manager.ExecuteNonQuery(trimmedCommand);
                }
            }
        }

        public override void ResetDb(ConnectionManager manager)
        {

        }
    }
}
