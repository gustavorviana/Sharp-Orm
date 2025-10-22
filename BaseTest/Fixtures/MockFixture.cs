using BaseTest.Mock;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace BaseTest.Fixtures
{
    public class MockFixture(QueryConfig config) : DbFixtureBase
    {
        public override QueryConfig GetConfig(bool safeConnection)
        {
            return config.Clone(safeConnection);
        }

        public override ConnectionCreator MakeConnectionCreator()
        {
            return new SingleConnectionCreator<MockConnection>(config.Clone(), null);
        }
    }

    public class MockFixture<Cnf> : DbFixtureBase where Cnf : QueryConfig, new()
    {
        public override QueryConfig GetConfig(bool safeConnection)
        {
            return new Cnf().Clone(safeConnection);
        }

        public override ConnectionCreator MakeConnectionCreator()
        {
            return new SingleConnectionCreator<MockConnection>(new Cnf(), null);
        }
    }
}
