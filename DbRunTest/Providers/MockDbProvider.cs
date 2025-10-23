using BaseTest.Mock;
using DbRunTest.DbInitializer;
using SharpOrm.Builder;

namespace DbRunTest.Providers
{
    internal class MockDbProvider(Type queryConfigType) : DbProvider(queryConfigType, typeof(MockConnection))
    {
        public MockDbProvider() : this(typeof(SqlServerQueryConfig))
        {

        }

        protected override DbInicializer? Initializer => null;

        protected override async Task<string> OnBuildAsync()
        {
            await Task.CompletedTask;
            return string.Empty;
        }
    }
}
