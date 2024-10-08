﻿using BaseTest.Fixtures;
using BaseTest.Mock;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace QueryTest.Fixtures
{
    public class DbFixture(QueryConfig config) : DbFixtureBase
    {
        protected override ConnectionCreator MakeConnectionCreator()
        {
            return new MultipleConnectionCreator<MockConnection>(config, null);
        }
    }

    public class DbFixture<Cnf> : DbFixtureBase where Cnf : QueryConfig, new()
    {
        protected override ConnectionCreator MakeConnectionCreator()
        {
            return new MultipleConnectionCreator<MockConnection>(new Cnf(), null);
        }
    }
}
