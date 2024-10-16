using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    internal class SqlServerInicializer : DbInicializer
    {
        public override void InitDb(ConnectionManager manager)
        {
            ExecuteScriptFile(manager, "SQlServer.sql");
        }

        public override void ResetDb(ConnectionManager manager)
        {

        }
    }
}
