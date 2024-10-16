using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    internal class MysqlInicializer : DbInicializer
    {
        public override void InitDb(ConnectionManager manager)
        {
            ExecuteScriptFile(manager, "Mysql.sql");
        }

        public override void ResetDb(ConnectionManager manager)
        {

        }
    }
}
