using SharpOrm.Builder;
using SharpOrm.Connection;

namespace BaseTest.Fixtures
{
    public abstract class DbFixtureBase : IDisposable
    {
        private bool disposed;

        public virtual ConnectionManager MakeManager(ConnectionCreator creator)
        {
            return new ConnectionManager(creator);
        }

        public abstract ConnectionCreator MakeConnectionCreator();

        public abstract QueryConfig GetConfig(bool safeConnection);

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            disposed = true;
        }

        ~DbFixtureBase()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
