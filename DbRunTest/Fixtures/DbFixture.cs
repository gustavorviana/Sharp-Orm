using BaseTest.Fixtures;
using DbRunTest.Utils;
using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.Connection;
using System.Data.Common;

namespace DbRunTest.Fixtures
{
    public class DbFixture<TConnection> : DbFixtureBase where TConnection : DbConnection, new()
    {
        private readonly WeakRefCollection<ConnectionManager> _managers = [];
        private readonly WeakRefCollection<ConnectionCreator> _creators = [];
        private readonly bool _safe;
        private bool _hasInit;

        public DbFixture() : this(true)
        {
        }

        protected DbFixture(bool safe)
        {
            _safe = safe;
        }

        public override ConnectionCreator MakeConnectionCreator()
        {
            var info = GetMap();
            var creator = new MultipleConnectionCreator<TConnection>(info.GetConfig(_safe), info.ConnString);
            _creators.Add(creator);
            return creator;
        }

        public override ConnectionManager MakeManager(ConnectionCreator creator)
        {
            var info = GetMap();
            var manager = new ConnectionManager(creator);
            manager.CheckConnection();
            if (!_hasInit)
            {
                _hasInit = true;
                info.Inicializer?.InitDb(manager);
            }
            _managers.Add(manager);
            return manager;
        }

        public override QueryConfig GetConfig(bool safeConnection)
        {
            return GetMap().GetConfig(safeConnection);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var info = GetMap();
                using var creator = MakeConnectionCreator();
                using var manager = MakeManager(creator);
                info.Inicializer?.ResetDb(manager);

                DisposeCollection(_managers);
                DisposeCollection(_creators);
            }

            base.Dispose(disposing);
        }

        private static ConnectionMap.ConnMapInfo GetMap()
        {
            return ConnectionMap.Get(typeof(TConnection));
        }

        private static void DisposeCollection<T>(WeakRefCollection<T> collection) where T : class, IDisposable
        {
            foreach (var item in collection.ToArray())
                try
                {
                    item.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
        }
    }

    public class UnsafeDbFixture<TConnection> : DbFixture<TConnection> where TConnection : DbConnection, new()
    {
        public UnsafeDbFixture() : base(false)
        {
        }
    }
}
