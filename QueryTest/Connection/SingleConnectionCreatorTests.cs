using BaseTest.Mock;
using NSubstitute;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;

namespace QueryTest.Connection
{
    public class SingleConnectionCreatorTests
    {
        private readonly QueryConfig mockConfig;

        public SingleConnectionCreatorTests()
        {
            mockConfig = Substitute.For<QueryConfig>();
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty);

            // Assert
            Assert.Equal(mockConfig, creator.Config);
            Assert.False(creator.Disposed);
        }

        [Fact]
        public void GetConnection_ShouldReturnSingleInstance()
        {
            // Arrange
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty);

            // Act
            var connection1 = creator.GetConnection();
            var connection2 = creator.GetConnection();

            // Assert
            Assert.Same(connection1, connection2);
            Assert.Equal(string.Empty, connection1.ConnectionString);
        }

        [Fact]
        public void GetConnection_WithAutoOpenConnection_ShouldOpenConnection()
        {
            // Arrange
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty)
            {
                AutoOpenConnection = true
            };

            // Act
            var connection = creator.GetConnection();

            // Assert
            Assert.Equal(ConnectionState.Open, connection.State);
        }

        [Fact]
        public void SafeDisposeConnection_WithManagedConnection_ShouldCloseConnection()
        {
            // Arrange
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty);
            var connection = creator.GetConnection();
            connection.Open();

            // Act
            creator.SafeDisposeConnection(connection);

            // Assert
            Assert.Equal(ConnectionState.Closed, connection.State);
        }

        [Fact]
        public void SafeDisposeConnection_WithDifferentConnection_ShouldNotAffectManagedConnection()
        {
            // Arrange
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty);
            var managedConnection = creator.GetConnection();
            managedConnection.Open();

            var differentConnection = new MockConnection();

            // Act
            creator.SafeDisposeConnection(differentConnection);

            // Assert
            Assert.Equal(ConnectionState.Open, managedConnection.State);
        }

        [Fact]
        public void Clone_ShouldCreateNewInstanceWithSameConfiguration()
        {
            // Arrange
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty)
            {
                AutoOpenConnection = true,
                Management = ConnectionManagement.LeaveOpen
            };

            // Act
            var cloned = creator.Clone() as SingleConnectionCreator<MockConnection>;

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(creator, cloned);
            Assert.Equal(creator.AutoOpenConnection, cloned.AutoOpenConnection);
            Assert.Equal(creator.Management, cloned.Management);
        }

        [Fact]
        public void Dispose_ShouldCloseAndDisposeConnection()
        {
            // Arrange
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty);
            var connection = creator.GetConnection();
            connection.Open();

            // Act
            creator.Dispose();

            // Assert
            Assert.True(creator.Disposed);
            Assert.Equal(ConnectionState.Closed, connection.State);
        }

        [Fact]
        public void GetConnection_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var creator = new SingleConnectionCreator<MockConnection>(mockConfig, string.Empty);
            creator.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => creator.GetConnection());
        }
    }
}
