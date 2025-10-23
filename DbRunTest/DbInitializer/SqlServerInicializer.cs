using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    internal class SqlServerInicializer : DbInicializer
    {
        public override Task InitAsync(ConnectionManager manager)
        {
            return ExecuteScriptFileAsync(manager, "SQlServer.sql");
        }

        public override Task ResetAsync(ConnectionManager manager)
        {
            return Task.CompletedTask;
        }
    }
}
