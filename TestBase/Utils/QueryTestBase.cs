using BaseTest.Fixtures;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public abstract class QueryTestBase : TestBase
    {
        protected readonly DbFixtureBase connection;
        protected virtual QueryConfig Config => this.connection.Creator.Config;
        protected virtual TranslationRegistry Translation => this.Config.Translation;

        public QueryTestBase(DbFixtureBase connection) : base(null)
        {
            ConnectionCreator.Default = connection.Creator;
            this.connection = connection;
        }

        public QueryTestBase(ITestOutputHelper? output, DbFixtureBase connection) : base(output)
        {
            ConnectionCreator.Default = connection.Creator;
            this.connection = connection;
        }

        protected override void Dispose(bool disposing)
        {
            ConnectionCreator.Default = null;
        }
    }
}
