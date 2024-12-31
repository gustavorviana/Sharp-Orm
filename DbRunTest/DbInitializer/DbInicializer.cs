using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    public abstract class DbInicializer
    {
        public abstract void InitDb(ConnectionManager manager);
        public abstract void ResetDb(ConnectionManager manager);

        protected void ExecuteScriptFile(ConnectionManager manager, string name)
        {
            manager.ExecuteNonQuery(File.ReadAllText("./Scripts/" + name));
        }
    }
}
