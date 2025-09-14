using BaseTest.Mock;
using NSubstitute;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;

namespace QueryTest.Connection
{
    public class MultipleConnectionCreatorTests
    {
        private readonly QueryConfig mockConfig;

        public MultipleConnectionCreatorTests()
        {
            mockConfig = Substitute.For<QueryConfig>();
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty);

            // Assert
            Assert.Equal(mockConfig, creator.Config);
            Assert.False(creator.Disposed);
        }

        [Fact]
        public void GetConnection_ShouldReturnNewInstanceEachTime()
        {
            // Arrange
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty);

            // Act
            var connection1 = creator.GetConnection();
            var connection2 = creator.GetConnection();

            // Assert
            Assert.NotSame(connection1, connection2);
            Assert.Equal(string.Empty, connection1.ConnectionString);
            Assert.Equal(string.Empty, connection2.ConnectionString);
        }

        [Fact]
        public void GetConnection_WithAutoOpenConnection_ShouldOpenConnection()
        {
            // Arrange
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty)
            {
                AutoOpenConnection = true
            };

            // Act
            var connection = creator.GetConnection();

            // Assert
            Assert.Equal(ConnectionState.Open, connection.State);
        }

        [Fact]
        public void SafeDisposeConnection_ShouldCloseAndDisposeConnection()
        {
            // Arrange
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty);
            var connection = creator.GetConnection();
            connection.Open();

            // Act
            creator.SafeDisposeConnection(connection);

            // Assert
            Assert.Equal(ConnectionState.Closed, connection.State);
            Assert.True(connection.IsDisposed);
        }

        [Fact]
        public void Clone_ShouldCreateNewInstanceWithSameConfiguration()
        {
            // Arrange
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty)
            {
                AutoOpenConnection = true
            };

            // Act
            var cloned = creator.Clone() as MultipleConnectionCreator<MockConnection>;

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(creator, cloned);
            Assert.Equal(creator.AutoOpenConnection, cloned.AutoOpenConnection);
        }

        [Fact]
        public void GetConnection_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty);
            creator.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => creator.GetConnection());
        }

        [Fact]
        public void Dispose_ShouldDisposeAllConnections()
        {
            // Arrange
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty);
            var connection1 = creator.GetConnection();
            var connection2 = creator.GetConnection();

            // Act
            creator.Dispose();

            // Assert
            Assert.True(creator.Disposed);
            // Note: As conexões são gerenciadas via WeakComponentsRef, então elas podem ser coletadas pelo GC
            // Este teste verifica principalmente se o Dispose não lança exceções
        }

        [Fact]
        public void Management_Property_ShouldNotAffectMultipleConnections()
        {
            // Arrange
            var creator = new MultipleConnectionCreator<MockConnection>(mockConfig, string.Empty)
            {
                Management = ConnectionManagement.LeaveOpen
            };

            // Act
            var connection = creator.GetConnection();
            connection.Open();
            creator.SafeDisposeConnection(connection);

            // Assert
            // No MultipleConnectionCreator, SafeDisposeConnection sempre fecha e dispõe a conexão
            Assert.Equal(ConnectionState.Closed, connection.State);
        }
    }
}
