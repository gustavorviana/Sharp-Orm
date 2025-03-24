using NSubstitute.ReceivedExtensions;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace QueryTest.Async
{
    public class CommandBuilderTests : DbCommandBaseTests
    {
        private readonly CommandBuilder _commandBuilder;
        private readonly ConnectionManager _connectionManager;

        public CommandBuilderTests()
        {
            _connectionManager = new ConnectionManager(new SqlServerQueryConfig(), Connection);
            _commandBuilder = new CommandBuilder(_connectionManager);
        }

        [Fact]
        public async Task SetBatchExpressionAsyncTest()
        {
            await _commandBuilder.SetExpressionAsync(
                new BatchSqlExpression([
                    new SqlExpression(""),
                    new SqlExpression(""),
                    new SqlExpression("")
                ])
            );

            _ = Connection.Received(Quantity.Exactly(1)).State;
            Connection.Received(Quantity.Exactly(1)).Open();
            Command.Received(Quantity.Exactly(2)).ExecuteNonQuery();
        }

        [Fact]
        public async Task SetExpressionAsyncTest()
        {
            await _commandBuilder.SetExpressionAsync(new SqlExpression(""));

            _ = Connection.Received(Quantity.Exactly(0)).State;
            Connection.Received(Quantity.Exactly(0)).Open();
            Command.Received(Quantity.Exactly(0)).ExecuteNonQuery();
        }

        [Fact]
        public async Task ExecuteNonQueryTest()
        {
            await _commandBuilder.ExecuteNonQueryAsync();
            await ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteNonQuery();
        }

        [Fact]
        public async Task ExecuteScalarTest()
        {
            await _commandBuilder.ExecuteScalarAsync();
            await ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteScalar();
        }

        [Fact]
        public async Task ExecuteTScalarTest()
        {
            await _commandBuilder.ExecuteScalarAsync<string>();
            await ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteScalar();
        }

        [Fact]
        public async Task ExecuteArrayScalarTest()
        {
            await _commandBuilder.ExecuteArrayScalarAsync<string>();
            await ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteReader();
            Connection.Received(Quantity.Exactly(1)).Close();
        }

        [Fact]
        public async Task ExecuteWithRecordsAffectedTest()
        {
            await _commandBuilder.ExecuteWithRecordsAffectedAsync();
            await ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteReader();
            _ = Reader.Received(Quantity.Exactly(1)).RecordsAffected;
        }

        [Fact]
        public async Task OpenIfNeeded()
        {
            await Connection.OpenIfNeededAsync();
            await ValidateOpenIfNeeded();
        }

        private async Task ValidateOpenIfNeeded(int statusCheck = 1)
        {
            _ = Connection.Received(Quantity.Exactly(statusCheck)).State;
            await Connection.Received(Quantity.Exactly(1)).OpenAsync();
        }
    }
}
