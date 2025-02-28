using SharpOrm.Connection;

namespace BaseTest.Fixtures
{
    public abstract class DbFixtureBase : IDisposable
    {
        private bool disposed;

        public ConnectionCreator Creator { get; }
        public ConnectionManager Manager { get; }

        public DbFixtureBase()
        {
            Creator = MakeConnectionCreator();
            Manager = MakeManager();
        }

        protected virtual ConnectionManager MakeManager()
        {
            return new ConnectionManager(Creator);
        }

        protected abstract ConnectionCreator MakeConnectionCreator();

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                Manager.Dispose();
                Creator.Dispose();
            }

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
