using BaseTest.Fixtures;
using DbRunTest.Providers;
using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.Connection;
using System.Data.Common;

namespace DbRunTest.Fixtures
{
#pragma warning disable CS8618
    public class DbFixture<TConnection> : DbFixtureBase where TConnection : DbConnection, new()
    {
        private readonly WeakRefCollection<ConnectionManager> _managers = [];
        private readonly WeakRefCollection<ConnectionCreator> _creators = [];
        protected DbProvider DbProvider { get; set; }

        public DbFixture()
        {
        }

        public override async Task InitializeAsync()
        {
            DbProvider = DbProvider.Get(typeof(TConnection));
            await DbProvider.BuildAsync();
            await base.InitializeAsync();
        }

        public override ConnectionCreator MakeConnectionCreator(bool safe)
        {
            var creator = DbProvider.GetConnectionCreator(true, safe);
            _creators.Add(creator);
            return creator;
        }

        public override ConnectionManager MakeManager(ConnectionCreator creator)
        {
            var manager = new ConnectionManager(creator);
            manager.CheckConnection();
            _managers.Add(manager);
            return manager;
        }

        public override QueryConfig GetConfig(bool safe)
        {
            return DbProvider.GetConfig(safe);
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                DisposeCollection(_managers);
                DisposeCollection(_creators);
            }

            await base.DisposeAsync(disposing);
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
#pragma warning restore CS8618 
}
