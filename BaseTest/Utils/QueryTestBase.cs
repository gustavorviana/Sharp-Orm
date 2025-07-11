﻿using BaseTest.Fixtures;
using BaseTest.Mock;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public abstract class QueryTestBase : TestBase
    {
        protected readonly DbFixtureBase fixture;
        protected virtual QueryConfig Config => fixture.Creator.Config;
        protected virtual TranslationRegistry Translation => Config.Translation;
        protected ConnectionManager Manager => fixture.Manager;
        public ConnectionCreator Creator => fixture.Creator;


        public QueryTestBase(DbFixtureBase connection) : base(null)
        {
            ConnectionCreator.Default = connection.Creator;
            fixture = connection;
        }

        public QueryTestBase(ITestOutputHelper? output, DbFixtureBase connection) : base(output)
        {
            ConnectionCreator.Default = connection.Creator;
            fixture = connection;
        }

        protected override void Dispose(bool disposing)
        {
            ConnectionCreator.Default = null;
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
