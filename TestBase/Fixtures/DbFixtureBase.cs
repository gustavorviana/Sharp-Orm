using SharpOrm.Connection;

namespace BaseTest.Fixtures
{
    public abstract class DbFixtureBase
    {
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
    }
}
