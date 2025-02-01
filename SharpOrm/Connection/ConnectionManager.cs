using SharpOrm.Builder;
using SharpOrm.Msg;
using System;
using System.Data;
using System.Data.Common;
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
        private ConnectionManagement management = ConnectionManagement.CloseOnDispose;
        internal readonly bool isMyTransaction = false;
        private readonly ConnectionCreator creator;
        private bool finishedTransaction = false;
        internal bool autoCommit = true;
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
            get => this.management;
            set
            {
                if (this.Transaction != null)
                    throw new InvalidOperationException("It's not possible to alter the connection management when a transaction is being used.");

                this.management = value;
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
                    if (creator?.Management == ConnectionManagement.LeaveOpen || this.management == ConnectionManagement.LeaveOpen)
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
        public ConnectionManager(bool openTransaction = false) : this(ConnectionCreator.Default, openTransaction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the default manager settings and the specified transaction isolation level.
        /// </summary>
        /// <param name="transactionIsolationLevel">The isolation level for the transaction.</param>
        public ConnectionManager(IsolationLevel transactionIsolationLevel) : this(ConnectionCreator.Default, transactionIsolationLevel)
        {
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
            this.creator = creator ?? throw new ArgumentNullException(nameof(creator), Messages.MissingCreator);
            this.CommandTimeout = creator.Config.CommandTimeout;
            this.Connection = this.creator.GetConnection();
            this.Config = creator.Config;

            this.Connection.Disposed += DisposeByConnection;
            this.management = GetManagement(creator, transactionIsolationLevel);
            if (!(transactionIsolationLevel is IsolationLevel isolationLevel))
                return;

            this.Transaction = this.Connection.OpenIfNeeded().BeginTransaction(isolationLevel);
            this.isMyTransaction = true;
        }

        private static ConnectionManagement GetManagement(ConnectionCreator creator, IsolationLevel? transactionIsolationLevel = null)
        {
            if (creator.Management == ConnectionManagement.LeaveOpen)
                return ConnectionManagement.LeaveOpen;

            return transactionIsolationLevel != null ? ConnectionManagement.CloseOnDispose : creator.Management;
        }

        private ConnectionManager(ConnectionCreator creator, DbTransaction transaction)
        {
            this.management = ConnectionManagement.CloseOnDispose;
            this.isMyTransaction = true;

            this.CommandTimeout = creator.Config.CommandTimeout;
            this.Connection = transaction.Connection;
            this.Transaction = transaction;
            this.Config = creator.Config;
            this.creator = creator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the specified transaction.
        /// </summary>
        /// <param name="config">The configuration used for the connection.</param>
        /// <param name="transaction">The <see cref="DbTransaction"/> to be used for this connection manager.</param>
        /// <exception cref="ArgumentNullException">Thrown if the transaction is null.</exception>
        /// <remarks>In this case, <see cref="this.Management"/> will not be considered for managing the connection; the original connection will remain open until manually closed.</remarks>
        public ConnectionManager(QueryConfig config, DbTransaction transaction) : this(config, transaction?.Connection)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.management = ConnectionManagement.LeaveOpen;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class using the specified connection.
        /// </summary>
        /// <param name="config">The configuration used for the connection.</param>
        /// <param name="connection">The <see cref="DbConnection"/> to be used for this connection manager.</param>
        /// <exception cref="ArgumentNullException">Thrown if the connection is null.</exception>
        public ConnectionManager(QueryConfig config, DbConnection connection)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.Config = config ?? throw new ArgumentNullException(nameof(config));

            this.Connection.Disposed += DisposeByConnection;
            this.CommandTimeout = config.CommandTimeout;
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
            if (this.disposed)
                throw new ObjectDisposedException(this.GetType().FullName);

            if (this.creator != null && this.Transaction != null)
                return new ConnectionManager(this.creator, this.Transaction).CopyOptionsFrom(this, false);

            if (this.creator != null)
                return new ConnectionManager(this.creator).CopyOptionsFrom(this);

            if (this.Transaction != null)
                return new ConnectionManager(this.Config, this.Transaction).CopyOptionsFrom(this);

            return new ConnectionManager(this.Config, this.Connection).CopyOptionsFrom(this);
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
            return this.BeginTransaction(IsolationLevel.Unspecified);
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
            if (this.Transaction != null)
                throw new InvalidOperationException("There is already an open transaction.");

            if (this.creator != null)
                return new ConnectionManager(this.creator, this.creator.GetConnection().OpenIfNeeded().BeginTransaction()).CopyOptionsFrom(this, false);

            return new ConnectionManager(Config, this.Connection.OpenIfNeeded().BeginTransaction(isolationLevel)).CopyOptionsFrom(this);
        }

        private ConnectionManager CopyOptionsFrom(ConnectionManager manager, bool copyManagement = true)
        {
            if (copyManagement) this.management = manager.management;
            this.CommandTimeout = manager.CommandTimeout;
            this.autoCommit = manager.autoCommit;
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
            try { this.Connection.Close(); } catch { }
        }

        #region Transaction
        /// <summary>
        /// If there is a transaction, commit the database transaction.
        /// </summary>
        /// <returns></returns>
        internal bool Commit()
        {
            if (this.finishedTransaction || this.Transaction is null)
                return false;

            this.finishedTransaction = true;

            try
            {
                this.Transaction.Commit();
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
            if (this.finishedTransaction || this.Transaction is null)
                return false;

            this.finishedTransaction = true;
            try
            {
                this.Transaction.Rollback();
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
                throw new InvalidOperationException($"It's not possible to start a transaction without setting a value for {nameof(ConnectionCreator)}.{nameof(ConnectionCreator.Default)}.");

            var manager = new ConnectionManager(creator, creator.GetConnection().OpenIfNeeded().BeginTransaction());

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
            if (this.Transaction == null || !this.isMyTransaction)
                return;

            if (this.autoCommit)
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
