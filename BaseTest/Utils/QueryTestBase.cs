using BaseTest.Fixtures;
using BaseTest.Mock;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public abstract class QueryTestBase : TestBase
    {
        private readonly DbFixtureBase _fixture;
        public ConnectionCreator Creator { get; private set; }
        protected virtual QueryConfig Config => Creator.Config;
        protected virtual TranslationRegistry Translation => Creator.Config.Translation;
        protected ConnectionManager Manager { get; }

        public QueryTestBase(DbFixtureBase fixture) : this(null, fixture)
        {
        }

        public QueryTestBase(ITestOutputHelper? output, DbFixtureBase fixture) : base(output)
        {
            _fixture = fixture;
            Creator = fixture.MakeConnectionCreator(true);
            Manager = fixture.MakeManager(Creator);
            ConnectionCreator.Default = Creator;
        }

        protected void ResetConfig()
        {
            TranslationRegistry.Default = new TranslationRegistry();
            Creator.Config = _fixture.GetConfig(Creator.Config.OnlySafeModifications);
        }

        protected void MakeUnsafe()
        {
            Creator.Config = Creator.Config.Clone(false);
        }

        protected void SetConnectionManagement(ConnectionManagement management)
        {
            Creator.Management = management;
            Manager.Management = management;
        }

        public override Task DisposeAsync()
        {
            ConnectionCreator.Default = null;
            ResetConfig();
            return base.DisposeAsync();
        }

        protected ConnectionManager GetManager(Action<QueryConfig> configure)
        {
            var config = Config.Clone();
            configure(config);
            return GetManager(config);
        }

        protected ConnectionManager GetManager(QueryConfig? config = null)
        {
            return config == null ? Manager : NewConnectionManager(config);
        }

        protected ConnectionManager NewConnectionManager(QueryConfig? config = null)
        {
            return new ConnectionManager(config ?? Config, Creator.GetConnection()) { Management = ConnectionManagement.CloseOnManagerDispose };
        }

        protected void SetMockConnectionVersion(Version? version = null)
        {
            if (Manager.Connection is not MockConnection connection)
                throw new ArgumentException("Current connection must be of type \"MockConnection\".");

            if (version == null)
                version = new Version(11, 0);

            connection.SetVersion(version);
        }
    }
}
