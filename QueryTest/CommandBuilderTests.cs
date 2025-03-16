using NSubstitute.ReceivedExtensions;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;

namespace QueryTest
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
        public void SetBatchExpressionTest()
        {
            _commandBuilder.SetExpression(
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
        public void SetExpressionTest()
        {
            _commandBuilder.SetExpression(new SqlExpression(""));

            _ = Connection.Received(Quantity.Exactly(0)).State;
            Connection.Received(Quantity.Exactly(0)).Open();
            Command.Received(Quantity.Exactly(0)).ExecuteNonQuery();
        }

        [Fact]
        public void ExecuteNonQueryTest()
        {
            _commandBuilder.ExecuteNonQuery();
            ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteNonQuery();
        }

        [Fact]
        public void ExecuteEnumerableTest()
        {
            _commandBuilder.ExecuteEnumerable<object>().FirstOrDefault();
            ValidateOpenIfNeeded(4);

            Command.Received(Quantity.Exactly(1)).ExecuteReader();
        }

        [Fact]
        public void ExecuteScalarTest()
        {
            _commandBuilder.ExecuteScalar();
            ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteScalar();
        }

        [Fact]
        public void ExecuteTScalarTest()
        {
            _commandBuilder.ExecuteScalar<string>();
            ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteScalar();
        }

        [Fact]
        public void ExecuteArrayScalarTest()
        {
            _commandBuilder.ExecuteArrayScalar<string>();
            ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteReader();
            Connection.Received(Quantity.Exactly(1)).Close();
        }

        [Fact]
        public void ExecuteReaderTest()
        {
            _commandBuilder.ExecuteReader();
            ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteReader();
        }

        [Fact]
        public void ExecuteWithRecordsAffectedTest()
        {
            _commandBuilder.ExecuteWithRecordsAffected();
            ValidateOpenIfNeeded();

            Command.Received(Quantity.Exactly(1)).ExecuteReader();
            _ = Reader.Received(Quantity.Exactly(1)).RecordsAffected;
        }

        [Fact]
        public void OpenIfNeeded()
        {
            Connection.OpenIfNeeded();
            ValidateOpenIfNeeded();
        }

        private void ValidateOpenIfNeeded(int statusCheck = 1)
        {
            _ = Connection.Received(Quantity.Exactly(statusCheck)).State;
            Connection.Received(Quantity.Exactly(1)).Open();
        }
    }
}
