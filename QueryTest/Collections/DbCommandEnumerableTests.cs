using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using NSubstitute;
using SharpOrm;
using SharpOrm.Collections;
using SharpOrm.Connection;
using System.Data.Common;

namespace QueryTest.Collections
{
    public class DbCommandEnumerableTests : DbMockTest
    {
        private readonly DbCommand _command;
        private readonly DbConnection _connection;

        public DbCommandEnumerableTests()
        {
            _connection = Substitute.For<DbConnection>();
            _command = Substitute.For<DbCommand>();
            _command.Connection = _connection;
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation);

            // Assert
            Assert.True(enumerable.DisposeCommand);
        }

        [Fact]
        public void GetEnumerator_ShouldOpen_connectionOnFirstCall()
        {
            // Arrange
            var reader = GetReader();
            _command.ExecuteReader().Returns(reader);
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation);

            // Act
            enumerable.GetEnumerator();

            // Assert
            _connection.Received(1).OpenIfNeeded();
            _command.Received(1).ExecuteReader();
        }

        [Fact]
        public void GetEnumerator_NonGeneric_ShouldOpen_connectionOnFirstCall()
        {
            // Arrange
            var reader = GetReader();
            _command.ExecuteReader().Returns(reader);
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation);

            // Act
            ((System.Collections.IEnumerable)enumerable).GetEnumerator();

            // Assert
            _connection.Received(1).OpenIfNeeded();
            _command.Received(1).ExecuteReader();
        }

        [Fact]
        public void SetCancellationToken_ShouldPassTokenToCommand()
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            // Act
            new DbCommandEnumerable<Customer>(_command, Translation, token: cancellationToken);

            // Assert
            _command.Received(1).SetCancellationToken(cancellationToken);
        }

        [Fact]
        public void DisposeCommand_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation);

            // Assert
            Assert.True(enumerable.DisposeCommand);
        }

        [Fact]
        public void DisposeCommand_CanBeSetToFalse()
        {
            // Arrange
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation);

            // Act
            enumerable.DisposeCommand = false;

            // Assert
            Assert.False(enumerable.DisposeCommand);
        }

        [Fact]
        public void ConnectionManagement_LeaveOpen_ShouldNotClose_connection()
        {
            // Arrange
            var reader = GetReader();
            _command.ExecuteReader().Returns(reader);
            _command.Transaction.Returns((DbTransaction)null);
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation, ConnectionManagement.LeaveOpen);

            // Act
            using (var enumerator = enumerable.GetEnumerator())
            {
                // Enumerator disposal happens here
            }

            // Assert
            _connection.DidNotReceive().Close();
        }

        [Fact]
        public void ConnectionManagement_CloseOnEndOperation_ShouldClose_connection()
        {
            // Arrange
            var reader = GetReader();
            _command.ExecuteReader().Returns(reader);
            _command.Transaction.Returns((DbTransaction)null);
            _connection.State.Returns(System.Data.ConnectionState.Closed, System.Data.ConnectionState.Open);
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation, ConnectionManagement.CloseOnEndOperation);

            // Act
            using (var enumerator = enumerable.GetEnumerator())
            {
                // Enumerator disposal happens here
            }

            // Assert
            _connection.Received(1).Close();
        }

        [Fact]
        public void ConnectionManagement_WithActiveTransaction_ShouldNotClose_connection()
        {
            // Arrange
            var reader = GetReader();
            var transaction = Substitute.For<DbTransaction>();
            _command.ExecuteReader().Returns(reader);
            _command.Transaction.Returns(transaction);
            _connection.State.Returns(System.Data.ConnectionState.Closed);
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation, ConnectionManagement.CloseOnEndOperation);

            // Act
            using (var enumerator = enumerable.GetEnumerator())
            {
                // Enumerator disposal happens here
            }

            // Assert
            _connection.DidNotReceive().Close();
        }

        [Fact]
        public void DisposeCommand_True_ShouldDisposeCommandOnEnumeratorDispose()
        {
            // Arrange
            var reader = GetReader();
            _command.ExecuteReader().Returns(reader);
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation)
            {
                DisposeCommand = true
            };

            // Act
            using (var enumerator = enumerable.GetEnumerator())
            {
                // Enumerator disposal happens here
            }

            // Assert
            _command.Received(1).Dispose();
        }

        [Fact]
        public void DisposeCommand_False_ShouldNotDisposeCommandOnEnumeratorDispose()
        {
            // Arrange
            var reader = GetReader();
            _command.ExecuteReader().Returns(reader);
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation)
            {
                DisposeCommand = false
            };

            // Act
            using (var enumerator = enumerable.GetEnumerator())
            {
                // Enumerator disposal happens here
            }

            // Assert
            _command.DidNotReceive().Dispose();
        }

        [Fact]
        public void GetEnumerator_With_connectionError_ShouldHandleGracefully()
        {
            // Arrange
            var reader = GetReader();
            _command.ExecuteReader().Returns(reader);
            _connection.When(x => x.Close()).Throws(new Exception("_connection error"));
            var enumerable = new DbCommandEnumerable<Customer>(_command, Translation, ConnectionManagement.CloseOnEndOperation);

            // Act & Assert - Should not throw exception even if connection close fails
            using (var enumerator = enumerable.GetEnumerator())
            {
                // Enumerator disposal happens here - should handle connection error gracefully
            }
        }

        private MockDataReader GetReader()
        {
            return new MockDataReader(i => new Row(), 0);
        }
    }
}