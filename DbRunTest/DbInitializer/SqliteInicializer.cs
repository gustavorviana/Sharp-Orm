using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    internal class SqliteInicializer : DbInicializer
    {
        public override void InitDb(ConnectionManager manager)
        {
            ExecuteScriptFile(manager, "Sqlite.sql");
        }

        public override void ResetDb(ConnectionManager manager)
        {

        }
    }
}
