using SharpOrm.Connection;

namespace DbRunTest.DbInitializer
{
    public abstract class DbInicializer
    {
        public abstract Task InitAsync(ConnectionManager manager);
        public abstract Task ResetAsync(ConnectionManager manager);

        protected static async Task ExecuteScriptFileAsync(ConnectionManager manager, string name)
        {
            var content = await File.ReadAllTextAsync("./Scripts/" + name);
            await manager.ExecuteNonQueryAsync(content);
        }
    }
}
