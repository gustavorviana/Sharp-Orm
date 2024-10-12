using BaseTest.Fixtures;
using SharpOrm.Connection;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public abstract class QueryTestBase : TestBase
    {
        protected readonly DbFixtureBase connection;

        public QueryTestBase(ITestOutputHelper output, DbFixtureBase connection) : base(output)
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
