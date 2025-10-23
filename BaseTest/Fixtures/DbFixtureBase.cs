using SharpOrm.Builder;
using SharpOrm.Connection;
using Xunit;

namespace BaseTest.Fixtures
{
    public abstract class DbFixtureBase : IAsyncLifetime
    {
        private bool disposed;

        public virtual ConnectionManager MakeManager(ConnectionCreator creator)
        {
            return new ConnectionManager(creator);
        }

        public abstract ConnectionCreator MakeConnectionCreator(bool safe);

        public abstract QueryConfig GetConfig(bool safe);

        protected virtual async Task DisposeAsync(bool disposing)
        {
            await Task.CompletedTask;
            if (disposed) return;

            disposed = true;
        }

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await DisposeAsync(disposing: true);
        }
    }
}
