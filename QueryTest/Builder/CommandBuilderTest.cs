using BaseTest.Mock;
using BaseTest.Utils;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;
using System.Data.Common;

namespace QueryTest.Builder
{
    public class CommandBuilderTest : DbMockTest
    {
        private readonly DbConnection _connection;
        private readonly DbCommand _command;
        private readonly ConnectionManager _manager;
        private readonly CommandBuilder _commandBuilder;

        public CommandBuilderTest()
        {
            _command = Substitute.For<DbCommand>();
            _command.Parameters.Returns(Substitute.For<DbParameterCollection>());
            _command.CreateParameter().Returns(Substitute.For<DbParameter>());
            _command.Connection = Substitute.For<DbConnection>();

            _connection = Substitute.For<DbConnection>();
            _connection.State.Returns(ConnectionState.Open);
            _connection.CreateCommand().Returns(_command);

            _manager = new ConnectionManager(Config, _connection)
            {
                Management = ConnectionManagement.CloseOnEndOperation
            };

            _commandBuilder = new CommandBuilder(_manager);
        }

        [Fact]
        public void SetExpression_WithSqlExpression_ShouldReturnSameInstance()
        {
            var expression = new SqlExpression("SELECT * FROM Users");

            var result = _commandBuilder.SetExpression(expression);

            Assert.Same(_commandBuilder, result);
        }

        [Fact]
        public void SetExpression_WithStringAndArgs_ShouldReturnSameInstance()
        {
            var result = _commandBuilder.SetExpression("SELECT * FROM Users WHERE Id = ?", 1);

            Assert.Same(_commandBuilder, result);
        }

        [Fact]
        public void SetExpression_WithBatchSqlExpression_ShouldExecuteAllButLast()
        {
            _connection.State.Returns(ConnectionState.Closed);
            var expressions = new SqlExpression[]
            {
                new SqlExpression("INSERT INTO Users VALUES (1)"),
                new SqlExpression("INSERT INTO Users VALUES (2)"),
                new SqlExpression("SELECT * FROM Users")
            };
            var batchExpression = new BatchSqlExpression(expressions);

            _commandBuilder.SetExpression(batchExpression);

            _connection.Received(1).Open();
            _command.Received(2).ExecuteNonQuery(); // Should execute first 2 expressions
        }

        [Fact]
        public void ExecuteNonQuery()
        {
            _command.ExecuteNonQuery().Returns(10);

            var result = _commandBuilder.ExecuteNonQuery();

            Assert.Equal(10, result);

            _connection.Received(1).Close();
        }

        [Fact]
        public async Task ExecuteNonQueryAsync()
        {
            _command.ExecuteNonQuery().Returns(10);

            var result = await _commandBuilder.ExecuteNonQueryAsync();

            Assert.Equal(10, result);

            _connection.Received(1).Close();
        }

        [Fact]
        public void ExecuteNonQuery_WhenConnectionClosed_ShouldOpenConnection()
        {
            _connection.State.Returns(ConnectionState.Closed);
            _command.ExecuteNonQuery().Returns(1);

            var result = _commandBuilder.ExecuteNonQuery();

            _connection.Received(1).Open();
            Assert.Equal(1, result);
        }

        [Fact]
        public void ExecuteScalar()
        {
            _command.ExecuteScalar().Returns("Result");

            var result = _commandBuilder.ExecuteScalar();

            Assert.Equal("Result", result);

            _connection.Received(1).Close();
        }

        [Fact]
        public async Task ExecuteScalarAsync()
        {
            _command.ExecuteScalar().Returns("Result");

            var result = await _commandBuilder.ExecuteScalarAsync();

            Assert.Equal("Result", result);

            _connection.Received(1).Close();
        }

        [Fact]
        public void ExecuteScalar_Generic_ShouldReturnTypedValue()
        {
            _command.ExecuteScalar().Returns("42");

            var result = _commandBuilder.ExecuteScalar<string>();

            Assert.Equal("42", result);

            _connection.Received(1).Close();
        }

        [Fact]
        public async Task ExecuteScalarAsync_Generic_ShouldReturnTypedValue()
        {
            _command.ExecuteScalar().Returns("42");

            var result = await _commandBuilder.ExecuteScalarAsync<string>();

            Assert.Equal("42", result);

            _connection.Received(1).Close();
        }

        [Fact]
        public void ExecuteArrayScalar()
        {
            var items = new[] { "1", "2", "3" };
            _command.ExecuteReader().Returns(GetDataReaderString(items));

            var result = _commandBuilder.ExecuteArrayScalar<string>();

            Assert.True(items.SequenceEqual(result));

            _connection.Received(1).Close();
        }

        [Fact]
        public async Task ExecuteArrayScalarAsync()
        {
            var items = new[] { "1", "2", "3" };
            _command.ExecuteReader().Returns(GetDataReaderString(items));

            var result = await _commandBuilder.ExecuteArrayScalarAsync<string>();

            Assert.True(items.SequenceEqual(result));

            _connection.Received(1).Close();
        }

        [Fact]
        public void ExecuteArrayScalar_EmptyResult_ShouldReturnEmptyArray()
        {
            _command.ExecuteReader().Returns(GetDataReaderString(new string[0]));

            var result = _commandBuilder.ExecuteArrayScalar<string>();

            Assert.NotNull(result);
            Assert.Empty(result);

            _connection.Received(1).Close();
        }

        [Fact]
        public void ExecuteReader()
        {
            var items = new[] { "1", "2", "3" };
            var reader = GetDataReaderString(items);
            _command.ExecuteReader().Returns(reader);

            var result = _commandBuilder.ExecuteReader();

            Assert.Same(reader, result);
        }

        [Fact]
        public void ExecuteReader_WithCommandBehavior_ShouldUseSpecifiedBehavior()
        {
            var reader = GetDataReaderString(new[] { "test" });
            _command.ExecuteReader(CommandBehavior.SingleResult).Returns(reader);

            var result = _commandBuilder.ExecuteReader(CommandBehavior.SingleResult);

            _command.Received(1).ExecuteReader(CommandBehavior.SingleResult);
            Assert.Same(reader, result);
        }

        [Fact]
        public void ExecuteWithRecordsAffected_ShouldReturnRecordsAffected()
        {
            var reader = Substitute.For<DbDataReader>();
            reader.RecordsAffected.Returns(1);
            _command.ExecuteReader(CommandBehavior.Default).Returns(reader);

            var result = _commandBuilder.ExecuteWithRecordsAffected();

            Assert.Equal(1, result);

            _connection.Received(1).Close();
        }

        [Fact]
        public async Task ExecuteWithRecordsAffectedAsync_ShouldReturnRecordsAffected()
        {
            var reader = Substitute.For<DbDataReader>();
            reader.RecordsAffected.Returns(3);
            _command.ExecuteReader(CommandBehavior.Default).Returns(reader);

            var result = await _commandBuilder.ExecuteWithRecordsAffectedAsync();

            Assert.Equal(3, result);

            _connection.Received(1).Close();
        }

        [Fact]
        public void SetExpressionWithAffectedRows_WithSingleExpression_ShouldReturnZero()
        {
            var expression = new SqlExpression("SELECT * FROM Users");

            var result = _commandBuilder.SetExpressionWithAffectedRows(expression);

            Assert.Equal(0, result);
        }

        [Fact]
        public void SetExpressionWithAffectedRows_WithBatchExpression_ShouldReturnAffectedRowsCount()
        {
            _connection.State.Returns(ConnectionState.Closed);
            var reader1 = Substitute.For<DbDataReader>();
            var reader2 = Substitute.For<DbDataReader>();
            reader1.RecordsAffected.Returns(3);
            reader2.RecordsAffected.Returns(2);

            _command.ExecuteReader(CommandBehavior.Default).Returns(reader1, reader2);

            var expressions = new SqlExpression[]
            {
                new SqlExpression("INSERT INTO Users VALUES (1)"),
                new SqlExpression("INSERT INTO Users VALUES (2)"),
                new SqlExpression("SELECT * FROM Users")
            };
            var batchExpression = new BatchSqlExpression(expressions);

            var result = _commandBuilder.SetExpressionWithAffectedRows(batchExpression);

            _connection.Received(1).Open();
            _command.Received(2).ExecuteReader(CommandBehavior.Default);
            Assert.Equal(5, result); // 3 + 2 records affected
        }

        [Fact]
        public async Task SetExpressionWithAffectedRowsAsync_ShouldWork()
        {
            var expression = new SqlExpression("SELECT * FROM Users");

            var result = await _commandBuilder.SetExpressionWithAffectedRowsAsync(expression);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ExecuteEnumerable_ShouldReturnDbCommandEnumerable()
        {

            var result = _commandBuilder.ExecuteEnumerable<string>();

            Assert.NotNull(result);
        }

        [Fact]
        public void ExecuteEnumerable_WithDisposeCommandFalse_ShouldSetDisposeCommandProperty()
        {

            var result = _commandBuilder.ExecuteEnumerable<string>(false);

            Assert.NotNull(result);
            Assert.False(result.DisposeCommand);
        }

        [Fact]
        public void LogQuery_WhenEnabled_ShouldBeTrue()
        {

            _commandBuilder.LogQuery = true;

            Assert.True(_commandBuilder.LogQuery);
        }

        [Fact]
        public void LogQuery_WhenDisabled_ShouldBeFalse()
        {

            _commandBuilder.LogQuery = false;

            Assert.False(_commandBuilder.LogQuery);
        }

        [Fact]
        public void AddCancellationToken_WithValidToken_ShouldReturnSameInstance()
        {
            var token = new CancellationToken();

            var result = _commandBuilder.AddCancellationToken(token);

            Assert.Same(_commandBuilder, result);
        }

        [Fact]
        public void AddCancellationToken_WithDefaultToken_ShouldReturnSameInstance()
        {

            var result = _commandBuilder.AddCancellationToken(default);

            Assert.Same(_commandBuilder, result);
        }

        [Fact]
        public void AddCancellationToken_WithCancelledToken_ShouldThrowOperationCancelledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => _commandBuilder.AddCancellationToken(cts.Token));
        }

        [Fact]
        public void ExecuteNonQuery_WhenExceptionThrown_ShouldPropagateException()
        {
            _command.ExecuteNonQuery().Throws(new InvalidOperationException("Test exception"));

            Assert.Throws<InvalidOperationException>(() => _commandBuilder.ExecuteNonQuery());

            _connection.Received(1).Close();
        }

        [Fact]
        public void ExecuteScalar_WhenExceptionThrown_ShouldPropagateException()
        {
            _command.ExecuteScalar().Throws(new DataException("Database error"));

            Assert.Throws<DataException>(() => _commandBuilder.ExecuteScalar());

            _connection.Received(1).Close();
        }

        [Fact]
        public void ExecuteReader_WhenExceptionThrown_ShouldPropagateException()
        {
            _command.ExecuteReader().Throws(new InvalidOperationException("Reader error"));

            Assert.Throws<InvalidOperationException>(() => _commandBuilder.ExecuteReader());
        }

        [Fact]
        public void ExecuteArrayScalar_WhenExceptionThrown_ShouldPropagateExceptionAndCloseConnection()
        {
            _command.ExecuteReader().Throws(new InvalidOperationException("Reader error"));

            Assert.Throws<InvalidOperationException>(() => _commandBuilder.ExecuteArrayScalar<string>());

            _connection.Received(1).Close();
        }

        private static MockDataReader GetDataReaderString(string[] items)
        {
            return new MockDataReader(i => new Row(new Cell("value", items[i])), items.Length);
        }
    }
}