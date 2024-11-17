using BaseTest.Mock;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace BaseTest.Fixtures
{
    public class MockFixture(QueryConfig config) : DbFixtureBase
    {
        protected override ConnectionCreator MakeConnectionCreator()
        {
            return new SingleConnectionCreator<MockConnection>(config, null);
        }
    }

    public class MockFixture<Cnf> : DbFixtureBase where Cnf : QueryConfig, new()
    {
        protected override ConnectionCreator MakeConnectionCreator()
        {
            return new SingleConnectionCreator<MockConnection>(new Cnf(), null);
        }
    }
}
