using SharpOrm.Builder;
using SharpOrm.Msg;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm.Connection
{
    /// <summary>
    /// Manages the database manager.
    /// </summary>
    public class ConnectionManager : IDisposableWithEvent
    {
        #region Fields/Properties
        /// <summary>
        /// Event that occurs when an error happens in the ConnectionManager.
        /// </summary>
        public event EventHandler<ConnectionExceptionEventArgs> OnError;

        private ConnectionManagement _management = ConnectionManagement.CloseOnDispose;
        internal readonly bool _isMyTransaction = false;
        private readonly ConnectionCreator creator;
        private bool finishedTransaction = false;
        internal bool _autoCommit = false;
        private bool disposed;

        /// <summary>
        /// Event that occurs when the ConnectionManager instance is disposed.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// Gets or sets the type of connection management. 
        /// Throws an <see cref="InvalidOperationException"/> if an attempt is made to alter the connection management while a transaction is active.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when attempting to change the connection management while a transaction is in use.</exception>
        public ConnectionManagement Management
        {
            get => _management;
            set
            {
                if (Transaction != null)
                    throw new InvalidOperationException(Messages.Connection.ManagementLockedByTransaction);

                _management = value;
            }
        }

        /// <summary>
        /// Gets the current database transaction, if one is active.
        /// </summary>
        public DbTransaction Transaction { get; }

        /// <summary>
        /// Gets the connection to the database.
        /// </summary>
        public DbConnection Connection { get; }

        /// <summary>
        /// Gets the configuration used for the connection.
        /// </summary>
        public QueryConfig Config { get; private set; }

        /// <summary>
        /// Gets or sets the maximum time, in seconds, the command should wait before throwing a timeout. The default is 30 seconds.
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Indicates whether the connection can be closed. 
        /// Returns true if there is no active transaction and the connection is not closed, and false otherwise.
        /// </summary>
        public bool CanClose
        {
            get
            {
                try
                {
                    if (creator?.Management == ConnectionManagement.LeaveOpen || _management == ConnectionManagement.LeaveOpen)
                        return false;

                    return this.Transaction is null && this.Connection.State != System.Data.ConnectionState.Closed;
                }
                catch
                {
                    return false;
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the default <see cref="ConnectionCreator"/>.
        /// Optionally opens a transaction.
        /// </summary>
        /// <param name="openTransaction">Indicates whether a transaction should be opened. Default is false.</param>
        public ConnectionManager(bool openTransaction = false, bool autoCommit = false) : this(ConnectionCreator.Default, openTransaction)
        {
            _autoCommit = autoCommit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the default manager settings and the specified transaction isolation level.
        /// </summary>
        /// <param name="transactionIsolationLevel">The isolation level for the transaction.</param>
        public ConnectionManager(IsolationLevel transactionIsolationLevel, bool autoCommit = false) : this(ConnectionCreator.Default, transactionIsolationLevel)
        {
            _autoCommit = autoCommit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the specified <see cref="ConnectionCreator"/>.
        /// </summary>
        /// <param name="creator">The <see cref="ConnectionCreator"/> responsible for creating database connections.</param>
        /// <exception cref="ArgumentNullException">Thrown if the creator is null.</exception>
        public ConnectionManager(ConnectionCreator creator) : this(creator, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the specified <see cref="ConnectionCreator"/> and optionally opens a transaction.
        /// </summary>
        /// <param name="creator">The <see cref="ConnectionCreator"/> responsible for creating database connections.</param>
        /// <param name="openTransaction">Indicates whether a transaction should be opened. Default is false.</param>
        /// <exception cref="ArgumentNullException">Thrown if the creator is null.</exception>
        public ConnectionManager(ConnectionCreator creator, bool openTransaction) : this(creator, openTransaction ? IsolationLevel.Unspecified : (IsolationLevel?)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the specified <see cref="ConnectionCreator"/> and transaction isolation level.
        /// </summary>
        /// <param name="creator">The <see cref="ConnectionCreator"/> responsible for creating database connections.</param>
        /// <param name="transactionIsolationLevel">The isolation level for the transaction, or null if no transaction is to be opened.</param>
        /// <exception cref="ArgumentNullException">Thrown if the creator is null.</exception>
        public ConnectionManager(ConnectionCreator creator, IsolationLevel? transactionIsolationLevel)
        {
            creator = creator ?? throw new ArgumentNullException(nameof(creator), Messages.MissingCreator);
            CommandTimeout = creator.Config.CommandTimeout;
            Connection = creator.GetConnection();
            Config = creator.Config;

            Connection.Disposed += DisposeByConnection;
            _management = GetManagement(creator, transactionIsolationLevel);
            if (!(transactionIsolationLevel is IsolationLevel isolationLevel))
                return;

            Transaction = Connection.OpenIfNeeded().BeginTransaction(isolationLevel);
            _autoCommit = creator.AutoCommit;
            _isMyTransaction = true;
        }

        private static ConnectionManagement GetManagement(ConnectionCreator creator, IsolationLevel? transactionIsolationLevel = null)
        {
            if (creator.Management == ConnectionManagement.LeaveOpen)
                return ConnectionManagement.LeaveOpen;

            return transactionIsolationLevel != null ? ConnectionManagement.CloseOnManagerDispose : creator.Management;
        }

        private ConnectionManager(ConnectionCreator creator, DbTransaction transaction, bool isMyTransaction)
        {
            _management = ConnectionManagement.CloseOnDispose;
            _isMyTransaction = true;

            CommandTimeout = creator.Config.CommandTimeout;
            Connection = transaction.Connection;
            _isMyTransaction = isMyTransaction;
            Transaction = transaction;
            Config = creator.Config;
            this.creator = creator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the specified transaction.
        /// </summary>
        /// <param name="config">The configuration used for the connection.</param>
        /// <param name="transaction">The <see cref="DbTransaction"/> to be used for this connection manager.</param>
        /// <exception cref="ArgumentNullException">Thrown if the transaction is null.</exception>
        /// <remarks>In this case, <see cref="this.Management"/> will not be considered for managing the connection; the original connection will remain open until manually closed.</remarks>
        public ConnectionManager(QueryConfig config, DbTransaction transaction, bool autoCommit = false) : this(config, transaction, false, ConnectionManagement.LeaveOpen)
        {
            _autoCommit = autoCommit;
        }

        private ConnectionManager(QueryConfig config, DbTransaction transaction, bool isMyTransaction, ConnectionManagement management) : this(config, transaction?.Connection)
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _isMyTransaction = isMyTransaction;
            _management = management;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the specified connection.
        /// </summary>
        /// <param name="config">The configuration used for the connection.</param>
        /// <param name="connection">The <see cref="DbConnection"/> to be used for this connection manager.</param>
        /// <exception cref="ArgumentNullException">Thrown if the connection is null.</exception>
        public ConnectionManager(QueryConfig config, DbConnection connection, bool autoCommit = false)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Config = config ?? throw new ArgumentNullException(nameof(config));

            Connection.Disposed += DisposeByConnection;
            CommandTimeout = config.CommandTimeout;
            _autoCommit = autoCommit;
        }

        private void DisposeByConnection(object sender, EventArgs e)
        {
            if (this.disposed) return;
            this.Connection.Disposed -= DisposeByConnection;
            GC.SuppressFinalize(this);
            this.disposed = true;

            this.Disposed?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        /// <summary>
        /// Attempts to close the connection using the reason "Operation completed".
        /// </summary>
        public void CloseByEndOperation()
        {
            if (this.Management == ConnectionManagement.CloseOnEndOperation && this.CanClose)
                this.Connection.Close();
        }

        /// <summary>
        /// Attempts to close the connection using the reason "The child is releasing its resources".
        /// </summary>
        public void CloseByDisposeChild()
        {
            if (this.Management == ConnectionManagement.CloseOnDispose && this.CanClose)
                this.Connection.Close();
        }

        /// <summary>
        /// Creates a clone of the current <see cref="ConnectionManager"/> instance with optional parameters to listen for disposal events and clone the configuration.
        /// </summary>
        /// <param name="listenDispose">Indicates whether to listen for disposal events. Default is true.</param>
        /// <param name="cloneConfig">Indicates whether to clone the configuration. Default is false.</param>
        /// <returns>A new <see cref="ConnectionManager"/> instance with the specified cloning options.</returns>
        public ConnectionManager Clone(bool listenDispose = true, bool cloneConfig = false, bool? safeOperations = null)
        {
            var clone = this.InternalClone();

            if (listenDispose)
                this.Disposed += (sender, e) => this.Dispose();

            if (cloneConfig)
                clone.Config = this.Config.Clone(safeOperations);

            return clone;
        }

        private ConnectionManager InternalClone()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (creator != null && Transaction != null)
                return new ConnectionManager(creator, Transaction, false).CopyOptionsFrom(this, false);

            if (creator != null)
                return new ConnectionManager(creator).CopyOptionsFrom(this);

            if (Transaction != null)
                return new ConnectionManager(Config, Transaction, _autoCommit).CopyOptionsFrom(this);

            return new ConnectionManager(Config, Connection, _autoCommit).CopyOptionsFrom(this);
        }

        /// <summary>
        /// Creates a new <see cref="ConnectionManager"/> with a transaction from the existing connection. 
        /// If the instance uses a <see cref="ConnectionCreator"/>, a new connection will be retrieved from it.
        /// </summary>
        /// <remarks>
        /// The transaction will automatically commit when calling <see cref="Dispose"/>, unless <see cref="Commit"/> or <see cref="Rollback"/> has been called before.
        /// </remarks>
        /// <returns>A new <see cref="ConnectionManager"/> instance with an active transaction.</returns>
        public ConnectionManager BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.Unspecified);
        }

        /// <summary>
        /// Creates a new <see cref="ConnectionManager"/> with a transaction from the existing connection, using the specified isolation level.
        /// If the instance uses a <see cref="ConnectionCreator"/>, a new connection will be retrieved from it.
        /// </summary>
        /// <param name="isolationLevel">The isolation level to be used for the transaction.</param>
        /// <remarks>
        /// The transaction will automatically commit when calling <see cref="Dispose"/>, unless <see cref="Commit"/> or <see cref="Rollback"/> has been called before.
        /// </remarks>
        /// <returns>A new <see cref="ConnectionManager"/> instance with an active transaction.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is already an open transaction.</exception>
        public ConnectionManager BeginTransaction(IsolationLevel isolationLevel)
        {
            if (Transaction != null)
                throw new InvalidOperationException(Messages.Connection.TransactionAlreadyOpen);

            var transaction = (creator?.GetConnection() ?? Connection).OpenIfNeeded().BeginTransaction(isolationLevel);
            var management = creator == null ? ConnectionManagement.CloseOnManagerDispose : creator.Management;

            return new ConnectionManager(Config, transaction, true, management).CopyOptionsFrom(this, false);
        }

        private ConnectionManager CopyOptionsFrom(ConnectionManager manager, bool copyManagement = true)
        {
            if (copyManagement) _management = manager._management;

            OnError = manager.OnError;
            CommandTimeout = manager.CommandTimeout;
            _autoCommit = manager._autoCommit;
            return this;
        }

        /// <summary>
        /// Checks if it is possible to connect to the database by attempting to open and then close the connection.
        /// </summary>
        public void CheckConnection()
        {
            if (this.Connection.State == System.Data.ConnectionState.Open)
                return;

            this.Connection.Open();
            try { this.Connection.Close(); } catch { }
        }

        /// <summary>
        /// Asynchronously checks if it is possible to connect to the database by attempting to open and then close the connection.
        /// </summary>
        /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task CheckConnectionAsync(CancellationToken token)
        {
            if (this.Connection.State == System.Data.ConnectionState.Open)
                return;

            await this.Connection.OpenAsync(token);

#if NET5_0_OR_GREATER
            try { await this.Connection.CloseAsync(); } catch { }
#else
            try { this.Connection.Close(); } catch { } 
#endif
        }

        internal void SignalException(Exception exception)
        {
            if (!(exception is OperationCanceledException))
                OnError?.Invoke(this, new ConnectionExceptionEventArgs(exception));
        }

        public Version GetServerVersion()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (creator != null)
                return creator.GetServerVersion();

            bool needClose = Connection.State != ConnectionState.Open;
            try
            {
                return Connection.OpenIfNeeded().GetVersion();
            }
            finally
            {
                if (needClose)
                    Connection.Close();
            }
        }

        /// <summary>  
        /// Retrieves a query object for the specified table name.  
        /// </summary>  
        /// <typeparam name="T">The type of the query object.</typeparam>  
        /// <param name="alias">The alias of the table.</param>  
        /// <returns>A query object for the specified table.</returns>  
        public virtual Query<T> GetQuery<T>(string alias = null)
        {
            return new Query<T>(alias, this);
        }

        /// <summary>  
        /// Retrieves a query object for the specified table name.  
        /// </summary>  
        /// <typeparam name="T">The type of the query object.</typeparam>  
        /// <param name="table">The name of the table.</param>  
        /// <returns>A query object for the specified table.</returns>  
        public virtual Query<T> GetQuery<T>(DbName table)
        {
            return new Query<T>(table, this);
        }

        /// <summary>
        /// Creates a query for the specified table.
        /// </summary>
        /// <param name="table">The name of the table as a <see cref="DbName"/> object.</param>
        /// <returns>A new instance of <see cref="Query"/> for the specified table.</returns>
        public virtual Query GetQuery(string table)
        {
            return GetQuery(new DbName(table));
        }

        /// <summary>
        /// Creates a query for the specified table.
        /// </summary>
        /// <param name="table">The name of the table as a <see cref="DbName"/> object.</param>
        /// <returns>A new instance of <see cref="Query"/> for the specified table.</returns>
        public virtual Query GetQuery(DbName table)
        {
            return new Query(table, this);
        }

        internal CommandBuilder GetCommand()
        {
            return new CommandBuilder(this);
        }

        internal CommandBuilder GetCommand(DataTranslation.TranslationRegistry registry, bool leaveOpen)
        {
            return new CommandBuilder(this, registry, leaveOpen);
        }

        #region Transaction
        /// <summary>
        /// If there is a transaction, commit the database transaction.
        /// </summary>
        /// <returns></returns>
        public bool Commit()
        {
            if (finishedTransaction || Transaction is null || !_isMyTransaction)
                return false;

            finishedTransaction = true;

            try
            {
                Transaction.Commit();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// If there is a transaction, rolls back a transaction from a pending state.
        /// </summary>
        /// <returns></returns>
        public bool Rollback()
        {
            if (finishedTransaction || Transaction is null || !_isMyTransaction)
                return false;

            finishedTransaction = true;
            try
            {
                Transaction.Rollback();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Executes a database transaction using the default <see cref="ConnectionCreator"/>.
        /// </summary>
        /// <param name="call">The action to be executed within the transaction, which takes a <see cref="ConnectionManager"/> as a parameter.</param>
        /// <exception cref="InvalidOperationException">Thrown if the default <see cref="ConnectionCreator.Default"/> is not set.</exception>
        public static void ExecuteTransaction(TransactionAction call)
        {
            if (!(ConnectionCreator.Default is ConnectionCreator creator))
                throw new InvalidOperationException(Messages.Connection.SetDefaultConnectionToStaticTransaction);

            var manager = new ConnectionManager(creator, creator.GetConnection().OpenIfNeeded().BeginTransaction(), true);

            try
            {
                call(manager);
                manager.Commit();
            }
            catch
            {
                manager.Rollback();
                throw;
            }
            finally
            {
                manager.Dispose();
            }
        }

        /// <summary>
        /// Executes a database transaction and returns a value of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value to be returned.</typeparam>
        /// <param name="func">The function to be executed within the transaction, which takes a <see cref="ConnectionManager"/> as a parameter and returns a value of type <typeparamref name="T"/>.</param>
        /// <returns>The value of type <typeparamref name="T"/> returned by the function after the transaction is executed.</returns>
        public static T ExecuteTransaction<T>(TransactionAction<T> func)
        {
            T value = default;
            ExecuteTransaction((transaction) => value = func(transaction));
            return value;
        }
        #endregion

        #region IDisposable
        ~ConnectionManager()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Disposes the current instance and releases unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.disposed) return;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            try
            {
                this.DisposeTransaction();
                this.DisposeConnection();
            }
            finally
            {
                disposed = true;
                this.Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void DisposeTransaction()
        {
            if (this.Transaction == null || !this._isMyTransaction)
                return;

            if (_autoCommit)
                try { this.Commit(); } catch { }

            try { this.Transaction.Dispose(); } catch { }
        }

        private void DisposeConnection()
        {
            if (this.Management == ConnectionManagement.LeaveOpen || this.Connection is null)
                return;

            try
            {
                if (this.CloseByCreator())
                    return;

                this.CloseConnection();

                if (this.Management == ConnectionManagement.DisposeOnManagerDispose)
                    this.Connection.Dispose();
            }
            catch
            { }
        }

        private bool CloseByCreator()
        {
            if (this.creator == null) return false;
            this.creator.SafeDisposeConnection(this.Connection);
            return true;
        }

        private void CloseConnection()
        {
            try
            {
                if (this.Connection.State == System.Data.ConnectionState.Open)
                    this.Connection.Close();
            }
            catch
            { }
        }
        #endregion
    }
}
