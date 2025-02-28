using BaseTest.Fixtures;
using DbRunTest.DbInitializer;
using DbRunTest.Utils;
using SharpOrm.Connection;
using System.Data.Common;

namespace DbRunTest.Fixtures
{
    public class DbFixture<Conn> : DbFixtureBase where Conn : DbConnection, new()
    {
        private DbInicializer? dbInicializer;

        protected override ConnectionCreator MakeConnectionCreator()
        {
            var info = GetMap();
            return new MultipleConnectionCreator<Conn>(info.GetConfig(), info.ConnString);
        }

        protected override ConnectionManager MakeManager()
        {
            var manager = base.MakeManager();
            manager.CheckConnection();

            dbInicializer?.InitDb(manager);
            return manager;
        }

        internal ConnectionMap.ConnMapInfo GetMap()
        {
            var info = ConnectionMap.Get(typeof(Conn));

            dbInicializer ??= info.Inicializer;

            return info;
        }

        protected override void Dispose(bool disposing)
        {
            dbInicializer?.ResetDb(Manager);
            base.Dispose(disposing);
        }
    }
}
