﻿using BaseTest.Fixtures;
using BaseTest.Mock;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using Xunit;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public class DbMockTest : QueryTestBase
    {
        protected MockConnection Connection => (MockConnection)fixture.Creator.GetConnection();

        public DbMockTest(ITestOutputHelper? output, DbFixtureBase connection) : base(output, connection)
        {
        }

        public DbMockTest(ITestOutputHelper? output) : base(output, new MockFixture(new SqlServerQueryConfig(false)))
        {

        }

        public DbMockTest() : base(null, new MockFixture(new SqlServerQueryConfig(false)))
        {

        }

        protected ConnectionManager GetManager(TranslationRegistry registry)
        {
            var config = Config.Clone();
            config.Translation = registry;

            return new ConnectionManager(config, Connection);
        }

        public static MockDataReader GetReader(params Cell[] cells)
        {
            return new MockDataReader(i => new Row(cells), cells.Length);
        }

        public static MockDataReader GetReader(Func<int, Cell[]> createCallback, int qtd)
        {
            return new MockDataReader(i => new Row(createCallback(i)), qtd);
        }

        protected static MockConnection GetNonQueryCommand(string expected)
        {
            var conn = new MockConnection();
            conn.OnExecuteNonQuery += (string query) =>
            {
                Assert.Equal(expected, query);
                return -1;
            };

            return conn;
        }
    }
}
