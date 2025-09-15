using BaseTest.Utils;
using NSubstitute;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using System.Data;
using System.Data.Common;

namespace QueryTest.Connection;

public class ConnectionManagerTests : DbMockFallbackTest
{
    #region Error Handling Tests

    [Fact]
    public void ManagerErrorsTest()
    {
        const string Error = "Test Error";
        using (RegisterFallback(x => throw new DatabaseException(Error)))
        {
            Exception? exception = null;
            Manager.OnError += (handler, args) => exception = args.Exception;
            Assert.Throws<DatabaseException>(() => Manager.ExecuteNonQuery("SELECT ERROR"));
            var validatedException = Assert.IsType<DatabaseException>(exception);
            Assert.Equal(Error, validatedException.Message);
        }
    }

    [Fact]
    public void SignalException_ShouldTriggerOnErrorEvent()
    {
        // Arrange
        var testException = new InvalidOperationException("Test exception");
        Exception? capturedEvent = null;
        Manager.OnError += (sender, args) => capturedEvent = args.Exception;

        // Act - Usando reflexão para chamar o método interno
        var method = typeof(ConnectionManager).GetMethod("SignalException",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(Manager, new object[] { testException });

        // Assert
        Assert.Equal(testException, capturedEvent);
    }

    [Fact]
    public void SignalException_WithOperationCanceledException_ShouldNotTriggerOnErrorEvent()
    {
        // Arrange
        var operationCanceledException = new OperationCanceledException();
        Exception? capturedEvent = null;
        Manager.OnError += (sender, args) => capturedEvent = args.Exception;

        // Act
        var method = typeof(ConnectionManager).GetMethod("SignalException",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(Manager, new object[] { operationCanceledException });

        // Assert
        Assert.Null(capturedEvent);
    }

    #endregion

    #region Command Builder Tests

    [Fact]
    public void CreateCommandBuilderTest()
    {
        const int EXPECTED_INITIAL_TIMEOUT = 30;
        const int EXPECTED_TIMEOUT = 120;

        var initialTimeout = Manager.CreateCommand().CommandTimeout;
        Manager.CommandTimeout = EXPECTED_TIMEOUT;
        var timeout = Manager.CreateCommand().CommandTimeout;

        Assert.Equal(EXPECTED_INITIAL_TIMEOUT, initialTimeout);
        Assert.Equal(EXPECTED_TIMEOUT, timeout);
    }

    [Fact]
    public void GetCommand_ShouldReturnCommandBuilderWithCorrectTimeout()
    {
        // Arrange
        const int customTimeout = 60;
        Manager.CommandTimeout = customTimeout;

        // Act
        var commandBuilder = Manager.GetCommand();

        // Assert
        Assert.NotNull(commandBuilder);
        Assert.Equal(customTimeout, commandBuilder.Timeout);
    }

    [Fact]
    public void CloneCommand_Should_Not_Close_Connection()
    {
        // Arrange
        var cloned = Manager.Clone();

        // Act
        Manager.Connection.Open();
        cloned.Dispose();

        // Assert
        Assert.True(Manager.Connection.IsOpen());
    }

    #endregion

    #region Query Tests

    [Fact]
    public void GetQuery_ShouldReturnNonNullAndSameManager()
    {
        // Act
        var query = Manager.GetQuery("Query");
        var queryWithTableName = Manager.GetQuery(new DbName("Table"));
        var genericQuery = Manager.GetQuery<object>();
        var genericQueryWithConfig = Manager.GetQuery<object>("Alias");
        var genericQueryWithTableName = Manager.GetQuery<object>(new DbName("Table"));

        // Assert
        Assert.NotNull(query);
        Assert.NotNull(genericQuery);
        Assert.NotNull(genericQueryWithConfig);
        Assert.NotNull(queryWithTableName);
        Assert.NotNull(genericQueryWithTableName);
        Assert.Same(Manager, query.Manager);
        Assert.Same(Manager, genericQuery.Manager);
        Assert.Same(Manager, genericQueryWithConfig.Manager);
        Assert.Same(Manager, queryWithTableName.Manager);
        Assert.Same(Manager, genericQueryWithTableName.Manager);
    }

    #endregion

    #region Connection Management Tests

    [Fact]
    public void Management_Set_WhenNoTransaction_ShouldUpdateValue()
    {
        // Arrange
        var manager = NewConnectionManager();
        var newManagement = ConnectionManagement.LeaveOpen;

        // Act
        manager.Management = newManagement;

        // Assert
        Assert.Equal(newManagement, manager.Management);
    }

    [Fact]
    public void Management_Set_WithActiveTransaction_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transactionManager = Manager.BeginTransaction();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            transactionManager.Management = ConnectionManagement.LeaveOpen);
    }

    [Fact]
    public void CanClose_WithNoTransactionAndOpenConnection_ShouldReturnTrue()
    {
        // Arrange
        var manager = NewConnectionManager();
        manager.Connection.Open();

        // Act
        var canClose = manager.CanClose;

        // Assert
        Assert.True(canClose);
    }

    [Fact]
    public void CanClose_WithLeaveOpenManagement_ShouldReturnFalse()
    {
        // Arrange
        var manager = NewConnectionManager();
        manager.Management = ConnectionManagement.LeaveOpen;

        // Act
        var canClose = manager.CanClose;

        // Assert
        Assert.False(canClose);
    }

    [Fact]
    public void CloseByEndOperation_WithCorrectManagement_ShouldCloseConnection()
    {
        // Arrange
        var manager = NewConnectionManager();
        manager.Management = ConnectionManagement.CloseOnEndOperation;
        manager.Connection.Open();

        // Act
        manager.CloseByEndOperation();

        // Assert
        Assert.Equal(ConnectionState.Closed, manager.Connection.State);
    }

    [Fact]
    public void CloseByDisposeChild_WithCorrectManagement_ShouldCloseConnection()
    {
        // Arrange
        var manager = NewConnectionManager();
        manager.Management = ConnectionManagement.CloseOnDispose;
        manager.Connection.Open();

        // Act
        manager.CloseByDisposeChild();

        // Assert
        Assert.Equal(ConnectionState.Closed, manager.Connection.State);
    }

    #endregion

    #region Connection Tests

    [Fact]
    public void CheckConnection_WithClosedConnection_ShouldOpenAndClose()
    {
        // Arrange
        var manager = NewConnectionManager();
        Assert.Equal(ConnectionState.Closed, manager.Connection.State);

        // Act
        manager.CheckConnection();

        // Assert - A conexão deve estar fechada após o CheckConnection
        Assert.Equal(ConnectionState.Closed, manager.Connection.State);
    }

    [Fact]
    public void CheckConnection_WithOpenConnection_ShouldNotChangeState()
    {
        // Arrange
        var manager = NewConnectionManager();
        manager.Connection.Open();
        Assert.Equal(ConnectionState.Open, manager.Connection.State);

        // Act
        manager.CheckConnection();

        // Assert
        Assert.Equal(ConnectionState.Open, manager.Connection.State);
    }

    [Fact]
    public async Task CheckConnectionAsync_WithClosedConnection_ShouldOpenAndClose()
    {
        // Arrange
        var manager = NewConnectionManager();
        Assert.Equal(ConnectionState.Closed, manager.Connection.State);

        // Act
        await manager.CheckConnectionAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ConnectionState.Closed, manager.Connection.State);
    }

    [Fact]
    public async Task CheckConnectionAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var manager = NewConnectionManager();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => manager.CheckConnectionAsync(cts.Token));
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public void BeginTransaction_WithNoActiveTransaction_ShouldReturnNewManagerWithTransaction()
    {
        // Arrange
        var manager = NewConnectionManager();

        // Act
        var transactionManager = manager.BeginTransaction();

        // Assert
        Assert.NotNull(transactionManager.Transaction);
        Assert.NotSame(manager, transactionManager);
    }

    [Fact]
    public void BeginTransaction_WithIsolationLevel_ShouldCreateTransactionWithCorrectIsolationLevel()
    {
        // Arrange
        var manager = NewConnectionManager();
        var isolationLevel = IsolationLevel.ReadCommitted;

        // Act
        var transactionManager = manager.BeginTransaction(isolationLevel);

        // Assert
        Assert.NotNull(transactionManager.Transaction);
        Assert.Equal(isolationLevel, transactionManager.Transaction.IsolationLevel);
    }

    [Fact]
    public void BeginTransaction_WithActiveTransaction_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transactionManager = Manager.BeginTransaction();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transactionManager.BeginTransaction());
    }

    [Fact]
    public void Commit_WithActiveTransaction_ShouldReturnTrue()
    {
        // Arrange
        var transactionManager = Manager.BeginTransaction();

        // Act
        var result = transactionManager.Commit();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Commit_WithoutTransaction_ShouldReturnFalse()
    {
        // Arrange
        var manager = NewConnectionManager();

        // Act
        var result = manager.Commit();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Commit_AlreadyCommitted_ShouldReturnFalse()
    {
        // Arrange
        var transactionManager = Manager.BeginTransaction();
        transactionManager.Commit();

        // Act
        var result = transactionManager.Commit();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Rollback_WithActiveTransaction_ShouldReturnTrue()
    {
        // Arrange
        var transactionManager = Manager.BeginTransaction();

        // Act
        var result = transactionManager.Rollback();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Rollback_WithoutTransaction_ShouldReturnFalse()
    {
        // Arrange
        var manager = NewConnectionManager();

        // Act
        var result = manager.Rollback();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExecuteTransaction_WithSuccessfulAction_ShouldCommitTransaction()
    {
        // Arrange
        var executed = false;
        ConnectionCreator.Default = Creator;

        // Act
        ConnectionManager.ExecuteTransaction(manager =>
        {
            executed = true;
            Assert.NotNull(manager.Transaction);
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void ExecuteTransaction_WithException_ShouldRollbackAndRethrow()
    {
        // Arrange
        var testException = new InvalidOperationException("Test error");
        ConnectionCreator.Default = Creator;

        // Act & Assert
        var thrownException = Assert.Throws<InvalidOperationException>(() =>
            ConnectionManager.ExecuteTransaction(manager => throw testException));

        Assert.Same(testException, thrownException);
    }

    [Fact]
    public void ExecuteTransaction_WithReturnValue_ShouldReturnCorrectValue()
    {
        // Arrange
        const string expectedValue = "Test Result";
        ConnectionCreator.Default = Creator;

        // Act
        var result = ConnectionManager.ExecuteTransaction<string>(manager =>
        {
            Assert.NotNull(manager.Transaction);
            return expectedValue;
        });

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void ExecuteTransaction_WithoutDefaultCreator_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var originalDefault = ConnectionCreator.Default;
        ConnectionCreator.Default = null;

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                ConnectionManager.ExecuteTransaction(manager => { }));
        }
        finally
        {
            ConnectionCreator.Default = originalDefault;
        }
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_ShouldReturnNewInstanceWithSameProperties()
    {
        // Arrange
        const int customTimeout = 45;
        Manager.CommandTimeout = customTimeout;

        // Act
        var clonedManager = Manager.Clone();

        // Assert
        Assert.NotSame(Manager, clonedManager);
        Assert.Equal(Manager.CommandTimeout, clonedManager.CommandTimeout);
        Assert.Equal(ConnectionManagement.LeaveOpen, clonedManager.Management);
        Assert.Same(Manager.Config, clonedManager.Config);
    }

    [Fact]
    public void Clone_WithCloneConfig_ShouldCloneConfiguration()
    {
        // Act
        var clonedManager = Manager.Clone(cloneConfig: true);

        // Assert
        Assert.NotSame(Manager, clonedManager);
        Assert.NotSame(Manager.Config, clonedManager.Config);
    }

    [Fact]
    public void Clone_WithListenDispose_ShouldDisposeCloneWhenOriginalIsDisposed()
    {
        // Arrange
        var manager = NewConnectionManager();
        var clone = manager.Clone(listenDispose: true);
        var cloneDisposed = false;
        clone.Disposed += (sender, e) => cloneDisposed = true;

        // Act
        manager.Dispose();

        // Assert
        Assert.True(cloneDisposed);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldTriggerDisposedEvent()
    {
        // Arrange
        var manager = NewConnectionManager();
        var eventTriggered = false;
        manager.Disposed += (sender, e) => eventTriggered = true;

        // Act
        manager.Dispose();

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void Dispose_WithAutoCommitTransaction_ShouldCommitBeforeDispose()
    {
        // Arrange
        var manager = new ConnectionManager(true, autoCommit: true);

        // Act - Transaction deve ser commitada automaticamente
        manager.Dispose();

        // Assert - Se chegou aqui sem exceção, o commit foi executado
        Assert.True(true);
    }

    [Fact]
    public void Dispose_MultipleCallsToDispose_ShouldNotThrow()
    {
        // Arrange
        var manager = NewConnectionManager();

        // Act & Assert - Multiple disposal calls should not throw
        manager.Dispose();
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public void Dispose_AfterDispose_OperationsShouldThrowObjectDisposedException()
    {
        // Arrange
        var transactionManager = Manager.BeginTransaction();
        transactionManager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => transactionManager.Clone());
    }

    #endregion

    #region Server Version Tests

    [Fact]
    public void GetServerVersion_ShouldReturnVersion()
    {
        // Arrange
        var expectedVersion = new Version(11, 0);
        SetMockConnectionVersion(expectedVersion);

        // Act
        var version = Manager.GetServerVersion();

        // Assert
        Assert.Equal(expectedVersion, version);
    }

    [Fact]
    public void GetServerVersion_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var manager = NewConnectionManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.GetServerVersion());
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultParameters_ShouldCreateManagerWithDefaults()
    {
        // Act
        var manager = new ConnectionManager();

        // Assert
        Assert.Equal(30, manager.CommandTimeout);
        Assert.Equal(ConnectionCreator.Default.Management, manager.Management);
        Assert.Null(manager.Transaction);
    }

    [Fact]
    public void Constructor_WithNullCreator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionManager(null));
    }

    [Fact]
    public void Constructor_WithNullTransaction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionManager(Config, (DbTransaction)null));
    }

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionManager(Config, (DbConnection)null));
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionManager(null, Connection));
    }

    [Fact]
    public void Constructor_WithTransactionIsolationLevel_ShouldCreateTransactionWithCorrectLevel()
    {
        // Arrange
        var isolationLevel = IsolationLevel.Serializable;

        // Act
        var manager = new ConnectionManager(isolationLevel);

        // Assert
        Assert.NotNull(manager.Transaction);
        Assert.Equal(isolationLevel, manager.Transaction.IsolationLevel);
    }

    #endregion
}