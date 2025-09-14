using NSubstitute;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;

namespace QueryTest.Connection
{
    public class ConnectionCreatorTests
    {
        private readonly QueryConfig mockConfig;

        public ConnectionCreatorTests()
        {
            mockConfig = Substitute.For<QueryConfig>();
        }

        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var creator = new TestConnectionCreator(mockConfig);

            // Assert
            Assert.False(creator.AutoCommit);
            Assert.False(creator.AutoOpenConnection);
            Assert.Equal(ConnectionManagement.CloseOnEndOperation, creator.Management);
            Assert.False(creator.Disposed);
            Assert.Equal(mockConfig, creator.Config);
        }

        [Fact]
        public void GetServerVersion_ShouldReturnCachedVersionOnSubsequentCalls()
        {
            // Arrange
            var mockConnection = Substitute.For<DbConnection>();
            var expectedVersion = new Version(1, 2, 3, 4);
            mockConnection.ServerVersion.Returns(expectedVersion.ToString());
            mockConnection.State.Returns(ConnectionState.Open);

            var creator = new TestConnectionCreator(mockConfig, mockConnection);

            // Act
            var version1 = creator.GetServerVersion();
            var version2 = creator.GetServerVersion();

            // Assert
            Assert.Equal(expectedVersion, version1);
            Assert.Equal(expectedVersion, version2);
            mockConnection.Received(1).GetVersion();
        }

        [Fact]
        public void GetServerVersion_WithForceRefresh_ShouldRefreshVersion()
        {
            // Arrange
            var mockConnection = Substitute.For<DbConnection>();
            var expectedVersion = new Version(1, 2, 3, 4);
            mockConnection.ServerVersion.Returns(expectedVersion.ToString());
            mockConnection.State.Returns(ConnectionState.Open);

            var creator = new TestConnectionCreator(mockConfig, mockConnection);

            // Act
            creator.GetServerVersion();
            creator.GetServerVersion(forceRefresh: true);

            // Assert
            mockConnection.Received(2).GetVersion();
        }

        [Fact]
        public void GetManager_ShouldReturnConnectionManager()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act
            using var manager = creator.GetManager();

            // Assert
            Assert.NotNull(manager);
            Assert.IsType<ConnectionManager>(manager);
        }

        [Fact]
        public void GetManager_WithIsolationLevel_ShouldReturnConnectionManagerWithTransaction()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);
            var isolationLevel = IsolationLevel.ReadCommitted;

            // Act
            using var manager = creator.GetManager(isolationLevel);

            // Assert
            Assert.NotNull(manager);
            Assert.IsType<ConnectionManager>(manager);
        }

        [Fact]
        public void ValidateConnectionType_WithNullType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => creator.TestValidateConnectionType(null));

            Assert.Equal("type", exception.ParamName);
        }

        [Fact]
        public void ValidateConnectionType_WithNonDbConnectionType_ShouldThrowArgumentException()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => creator.TestValidateConnectionType(typeof(string)));
            Assert.Contains("must inherit from DbConnection", exception.Message);
        }

        [Fact]
        public void ValidateConnectionType_WithAbstractType_ShouldThrowArgumentException()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => creator.TestValidateConnectionType(typeof(DbConnection)));
            Assert.Contains("cannot be abstract", exception.Message);
        }

        [Fact]
        public void ValidateConnectionType_WithInterfaceType_ShouldThrowArgumentException()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => creator.TestValidateConnectionType(typeof(IDbConnection)));
            Assert.Contains("must be a class", exception.Message);
        }

        [Fact]
        public void ValidateConnectionType_WithValidType_ShouldNotThrow()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act & Assert (não deve lançar exceção)
            creator.TestValidateConnectionType(typeof(SqlConnection));
        }

        [Fact]
        public void Dispose_ShouldSetDisposedToTrue()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act
            creator.Dispose();

            // Assert
            Assert.True(creator.Disposed);
        }

        [Fact]
        public void GetConnection_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);
            creator.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => creator.GetConnection());
        }

        [Fact]
        public void Clone_ShouldReturnNewInstanceWithSameProperties()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig)
            {
                AutoCommit = true,
                AutoOpenConnection = true,
                Management = ConnectionManagement.LeaveOpen
            };

            // Act
            var cloned = creator.Clone();

            // Assert
            Assert.NotSame(creator, cloned);
            Assert.Equal(creator.AutoCommit, cloned.AutoCommit);
            Assert.Equal(creator.AutoOpenConnection, cloned.AutoOpenConnection);
            Assert.Equal(creator.Management, cloned.Management);
        }

        [Fact]
        public void DefaultProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var creator = new TestConnectionCreator(mockConfig);

            // Act
            ConnectionCreator.Default = creator;

            // Assert
            Assert.Equal(creator, ConnectionCreator.Default);
        }
    }
}


// Extensão para permitir testar o método protegido ValidateConnectionType
file static class TestConnectionCreatorExtensions
{
    public static void TestValidateConnectionType(this TestConnectionCreator creator, Type type)
    {
        try
        {
            var method = typeof(ConnectionCreator).GetMethod("ValidateConnectionType", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(creator, new[] { type });
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException;
        }
    }
}

file class TestConnectionCreator : ConnectionCreator
{
    private readonly DbConnection mockConnection;
    private readonly QueryConfig config;

    public TestConnectionCreator(QueryConfig config, DbConnection mockConnection = null)
    {
        this.config = config;
        this.mockConnection = mockConnection ?? Substitute.For<DbConnection>();
        Config = config;
    }

    public override DbConnection GetConnection()
    {
        ThrowIfDisposed();
        return mockConnection;
    }

    public override void SafeDisposeConnection(DbConnection connection)
    {
        connection?.Dispose();
    }

    public override ConnectionCreator Clone()
    {
        return new TestConnectionCreator(config, mockConnection)
        {
            AutoCommit = AutoCommit,
            AutoOpenConnection = AutoOpenConnection,
            Management = Management
        };
    }
}

