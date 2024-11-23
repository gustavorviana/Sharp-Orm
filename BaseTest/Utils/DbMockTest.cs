using BaseTest.Fixtures;
using BaseTest.Mock;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using Xunit;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public class DbMockTest : QueryTestBase
    {
        protected MockConnection Connection => (MockConnection)this.fixture.Creator.GetConnection();
        protected ConnectionManager Manager => this.fixture.Manager;

        public DbMockTest(ITestOutputHelper? output, DbFixtureBase connection) : base(output, connection)
        {
        }

        public DbMockTest(ITestOutputHelper? output) : base(output, new MockFixture(new SqlServerQueryConfig()))
        {

        }

        public DbMockTest() : base(null, new MockFixture(new SqlServerQueryConfig()))
        {

        }

        protected ConnectionManager GetManager(QueryConfig config)
        {
            return new ConnectionManager(config, this.Connection);
        }


        public static MockDataReader GetReader(params Cell[] cells)
        {
            return new MockDataReader(i => new Row(cells), cells.Length);
        }

        public static MockDataReader GetReader(Func<int, Cell[]> createCallback, int qtd)
        {
            return new MockDataReader(i => new Row(createCallback(i)), qtd);
        }

        public static MockDataReader GetReader<T>(int qtd, Func<int, T> createCallback, bool loadForeign)
        {
            Type type = typeof(T);
            return new MockDataReader(i => Row.Parse(createCallback(i), type, true, loadForeign), qtd);
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
