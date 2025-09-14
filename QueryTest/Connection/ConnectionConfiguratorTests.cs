using BaseTest.Mock;
using NSubstitute;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data.SqlClient;

namespace QueryTest.Connection
{
    public class ConnectionConfiguratorTests : IDisposable
    {
        private readonly QueryConfig _config;
        private readonly string _connectionString;
        private readonly TestConnectionConfigurator _configurator;
        private ConnectionCreator? _creator;

        public ConnectionConfiguratorTests()
        {
            _config = Substitute.For<QueryConfig>();
            _connectionString = "Server=localhost;Database=Test;";
            _configurator = new TestConnectionConfigurator();
        }

        public void Dispose()
        {
            _creator?.Dispose();
        }

        [Fact]
        public void GetConnection_WithConfigurator_CallsConfigureOnFirstAccess()
        {
            // Arrange
            _creator = new SingleConnectionCreator(_config, _connectionString, _configurator);

            // Act
            var connection = _creator.GetConnection();

            // Assert
            Assert.True(_configurator.ConfigureCalled);
            Assert.NotNull(_configurator.LastConfiguredConnection);
            Assert.IsType<SqlConnection>(_configurator.LastConfiguredConnection);
        }

        [Fact]
        public void GetConnection_WithConfigurator_CallsConfigureOnlyOnce()
        {
            // Arrange
            _creator = new SingleConnectionCreator(_config, _connectionString, _configurator);

            // Act
            var connection1 = _creator.GetConnection();
            var connection2 = _creator.GetConnection();

            // Assert
            Assert.Equal(1, _configurator.ConfigureCallCount);
            Assert.Same(connection1, connection2);
        }

        [Fact]
        public void GetConnection_WithoutConfigurator_DoesNotThrow()
        {
            // Arrange
            _creator = new SingleConnectionCreator(_config, _connectionString);

            // Act & Assert
            var connection = _creator.GetConnection();
            Assert.NotNull(connection);
            Assert.IsType<SqlConnection>(connection);
        }

        [Fact]
        public void GetConnection_WithIncompatibleConfigurator_ThrowsNotSupportedException()
        {
            // Arrange
            var incompatibleConfigurator = new IncompatibleConnectionConfigurator();
            _creator = new SingleConnectionCreator(_config, _connectionString, incompatibleConfigurator);

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => _creator.GetConnection());
            Assert.Equal("Connection type 'SqlConnection' is not supported. Expected type: 'MockConnection'.", exception.Message);
        }

        [Fact]
        public void SingleConnectionCreator_Clone_WithConfigurator_ClonesConfiguratorReference()
        {
            // Arrange
            _creator = new SingleConnectionCreator(_config, _connectionString, _configurator);

            // Act
            var clonedCreator = _creator.Clone() as SingleConnectionCreator;
            var originalConnection = _creator.GetConnection();
            var clonedConnection = clonedCreator.GetConnection();

            // Assert
            Assert.NotSame(_creator, clonedCreator);
            Assert.NotSame(originalConnection, clonedConnection);
            Assert.Equal(2, _configurator.ConfigureCallCount); // Both instances call configure
        }

        [Fact]
        public void SingleConnectionCreatorT_Clone_WithConfigurator_ClonesConfiguratorReference()
        {
            // Arrange
            _creator = new SingleConnectionCreator<SqlConnection>(_config, _connectionString, _configurator);

            // Act
            var clonedCreator = _creator.Clone() as SingleConnectionCreator;
            var originalConnection = _creator.GetConnection();
            var clonedConnection = clonedCreator.GetConnection();

            // Assert
            Assert.NotSame(_creator, clonedCreator);
            Assert.NotSame(originalConnection, clonedConnection);
            Assert.Equal(2, _configurator.ConfigureCallCount); // Both instances call configure
        }

        [Fact]
        public void MultipleConnectionCreator_Clone_WithConfigurator_ClonesConfiguratorReference()
        {
            // Arrange
            _creator = new MultipleConnectionCreator(_config, _connectionString, _configurator);

            // Act
            var clonedCreator = _creator.Clone() as MultipleConnectionCreator;
            var originalConnection = _creator.GetConnection();
            var clonedConnection = clonedCreator.GetConnection();

            // Assert
            Assert.NotSame(_creator, clonedCreator);
            Assert.NotSame(originalConnection, clonedConnection);
            Assert.Equal(2, _configurator.ConfigureCallCount); // Both instances call configure
        }

        [Fact]
        public void MultipleConnectionCreatorT_Clone_WithConfigurator_ClonesConfiguratorReference()
        {
            // Arrange
            _creator = new MultipleConnectionCreator<SqlConnection>(_config, _connectionString, _configurator);

            // Act
            var clonedCreator = _creator.Clone() as MultipleConnectionCreator;
            var originalConnection = _creator.GetConnection();
            var clonedConnection = clonedCreator.GetConnection();

            // Assert
            Assert.NotSame(_creator, clonedCreator);
            Assert.NotSame(originalConnection, clonedConnection);
            Assert.Equal(2, _configurator.ConfigureCallCount); // Both instances call configure
        }

        [Fact]
        public void GetConnection_AfterConnectionDisposed_RecreatesAndReconfigures()
        {
            // Arrange
            _creator = new SingleConnectionCreator(_config, _connectionString, _configurator);
            var firstConnection = _creator.GetConnection();

            // Act
            firstConnection.Dispose(); // This should trigger OnConnectionDisposed
            var secondConnection = _creator.GetConnection();

            // Assert
            Assert.NotSame(firstConnection, secondConnection);
            Assert.Equal(2, _configurator.ConfigureCallCount);
        }

        [Fact]
        public void Constructor_WithNullConfigurator_DoesNotThrow()
        {
            // Act & Assert
            _creator = new SingleConnectionCreator(_config, _connectionString, null);
            Assert.NotNull(_creator);
        }

        // Test implementation classes
        private class TestConnectionConfigurator : ConnectionConfigurator<SqlConnection>
        {
            public bool ConfigureCalled { get; private set; }
            public int ConfigureCallCount { get; private set; }
            public SqlConnection LastConfiguredConnection { get; private set; }

            public override void Configure(SqlConnection connection)
            {
                ConfigureCalled = true;
                ConfigureCallCount++;
                LastConfiguredConnection = connection;

                // Simulate some configuration
                if (!string.IsNullOrEmpty(connection.ConnectionString))
                {
                    // Could set additional properties here
                }
            }
        }

        private class IncompatibleConnectionConfigurator : ConnectionConfigurator<MockConnection>
        {
            public override void Configure(MockConnection connection)
            {
                // This will never be called in our tests
            }
        }
    }
}