using BaseTest.Fixtures;
using DbRunTest.Utils;
using SharpOrm.Connection;
using System.Data.Common;

namespace DbRunTest.Fixtures
{
    public class DbFixture<Conn> : DbFixtureBase where Conn : DbConnection, new()
    {
        protected override ConnectionCreator MakeConnectionCreator()
        {
            var info = ConnectionMap.Get(typeof(Conn));
            return new MultipleConnectionCreator<Conn>(info.Config, info.ConnString);
        }

        protected override ConnectionManager MakeManager()
        {
            var manager = base.MakeManager();
            manager.CheckConnection();
            return manager;
        }
    }
}
