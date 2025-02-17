using BaseTest.Mock;
using BaseTest.Utils;
using NSubstitute;
using SharpOrm;
using SharpOrm.Builder;
using System.Data.Common;

namespace QueryTest.Builder
{
    public class CommandBuilderTest : DbMockTest
    {
        private readonly DbConnection _connection;
        private readonly DbCommand _command;

        public CommandBuilderTest()
        {
            _command = Substitute.For<DbCommand>();
            _command.Connection = new MockConnection();

            _connection = Substitute.For<DbConnection>();
            _connection.State.Returns(System.Data.ConnectionState.Open);
            _connection.CreateCommand().Returns(_command);
        }

        [Fact]
        public void ExecuteNonQuery()
        {
            _command.ExecuteNonQuery().Returns(10);

            Assert.Equal(10, GetBuilder().ExecuteNonQuery());
        }

        [Fact]
        public async Task ExecuteNonQueryAsync()
        {
            _command.ExecuteNonQuery().Returns(10);

            Assert.Equal(10, await GetBuilder().ExecuteNonQueryAsync());
        }

        [Fact]
        public void ExecuteScalar()
        {
            _command.ExecuteScalar().Returns("Result");

            Assert.Equal("Result", GetBuilder().ExecuteScalar());
        }

        [Fact]
        public async Task ExecuteScalarAsync()
        {
            _command.ExecuteScalar().Returns("Result");

            Assert.Equal("Result", await GetBuilder().ExecuteScalarAsync());
        }

        [Fact]
        public void ExecuteArrayScalar()
        {
            var items = new[] { "1", "2", "3" };
            _command.ExecuteReader().Returns(GetDataReaderString(items));

            Assert.True(items.SequenceEqual(GetBuilder().ExecuteArrayScalar<string>()));
        }

        [Fact]
        public async Task ExecuteArrayScalarAsync()
        {
            var items = new[] { "1", "2", "3" };
            _command.ExecuteReader().Returns(GetDataReaderString(items));

            Assert.True(items.SequenceEqual(await GetBuilder().ExecuteArrayScalarAsync<string>()));
        }

        [Fact]
        public void ExecuteReader()
        {
            var items = new[] { "1", "2", "3" };
            _command.ExecuteReader().Returns(GetDataReaderString(items));

            GetBuilder().ExecuteReader();
        }

        private static MockDataReader GetDataReaderString(string[] items)
        {
            return new MockDataReader(i => new Row(new Cell("value", items[i])), items.Length);
        }

        private CommandBuilder GetBuilder()
        {
            return new CommandBuilder(new SharpOrm.Connection.ConnectionManager(Config, _connection));
        }
    }
}
