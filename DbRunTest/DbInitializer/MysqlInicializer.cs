using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    internal class MysqlInicializer : DbInicializer
    {
        public override Task InitAsync(ConnectionManager manager)
        {
            return ExecuteScriptFileAsync(manager, "Mysql.sql");
        }

        public override Task ResetAsync(ConnectionManager manager)
        {
            return Task.CompletedTask;
        }
    }
}
