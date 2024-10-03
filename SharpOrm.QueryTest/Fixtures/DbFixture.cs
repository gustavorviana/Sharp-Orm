using SharpOrm.Connection;

namespace SharpOrm.QueryTest.Fixtures
{
    public abstract class DbFixture
    {
        public ConnectionCreator Creator { get; }
        public ConnectionManager Manager { get; }

        public DbFixture()
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
