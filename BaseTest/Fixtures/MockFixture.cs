using BaseTest.Mock;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace BaseTest.Fixtures
{
    public class MockFixture : DbFixtureBase
    {
        private readonly QueryConfig _config;

        public MockFixture() : this(new SqlServerQueryConfig())
        {

        }

        private MockFixture(QueryConfig config)
        {
            _config = config;
        }

        public static MockFixture FromConfig(QueryConfig config)
        {
            return new MockFixture(config);
        }

        public override ConnectionCreator MakeConnectionCreator(bool safe)
        {
            return new SingleConnectionCreator<MockConnection>(GetConfig(safe), null);
        }

        public override QueryConfig GetConfig(bool safe)
        {
            if (safe == _config.OnlySafeModifications)
                return _config;

            return _config.Clone(safe);
        }
    }

    public class MockFixture<Cnf> : DbFixtureBase where Cnf : QueryConfig, new()
    {
        public MockFixture()
        {

        }

        public override QueryConfig GetConfig(bool safeConnection)
        {
            return new Cnf().Clone(safeConnection);
        }

        public override ConnectionCreator MakeConnectionCreator(bool safe)
        {
            return new SingleConnectionCreator<MockConnection>(new Cnf().Clone(safe), null);
        }
    }
}
