using SharpOrm.Builder;
using System;
using System.Data.Common;

namespace SharpOrm.Connection
{
    /// <summary>
    /// Manages the database manager.
    /// </summary>
    public class ConnectionManager : IDisposable
    {
        #region Fields/Properties
        private ConnectionManagement _management = ConnectionManagement.CloseOnDispose;
        private readonly bool isMyTransaction = false;
        private readonly ConnectionCreator creator;
        private bool finishedTransaction = false;
        private bool disposed;

        /// <summary>
        /// Type of connection management.
        /// </summary>
        public ConnectionManagement Management
        {
            get => this._management;
            set
            {
                if (this.Transaction != null)
                    throw new InvalidOperationException("It's not possible to alter the connection management when a transaction is being used.");

                this._management = value;
            }
        }

        /// <summary>
        /// Database transaction.
        /// </summary>
        public DbTransaction Transaction { get; }
        /// <summary>
        /// Connection to the database.
        /// </summary>
        public DbConnection Connection { get; }
        /// <summary>
        /// Configuration used for the connection.
        /// </summary>
        public QueryConfig Config { get; }
        /// <summary>
        /// Maximum time the command should wait before throwing a timeout.
        /// </summary>
        public int CommandTimeout { get; set; } = 30;
        /// <summary>
        /// It will be true if there is no transaction and the connection status is not closed.
        /// </summary>
        public bool CanClose
        {
            get => this.Transaction is null && this.Connection.State != System.Data.ConnectionState.Closed;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance using the default manager settings.
        /// </summary>
        public ConnectionManager() : this(ConnectionCreator.Default)
        {
        }

        /// <summary>
        /// Creates an instance using the default manager settings.
        /// </summary>
        public ConnectionManager(QueryConfig config) : this(ConnectionCreator.Default)
        {
            this.Config = config;
        }

        /// <summary>
        /// Creates an instance using a manager builder.
        /// </summary>
        /// <param name="creator"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConnectionManager(ConnectionCreator creator)
        {
            this.creator = creator ?? throw new ArgumentNullException(nameof(creator), Messages.MissingCreator);
            this.CommandTimeout = creator.Config.CommandTimeout;
            this.Connection = this.creator.GetConnection();
            this.Config = creator.Config;
        }

        private ConnectionManager(ConnectionCreator creator, DbTransaction transaction)
        {
            this._management = ConnectionManagement.CloseOnDispose;
            this.isMyTransaction = true;

            this.CommandTimeout = creator.Config.CommandTimeout;
            this.Transaction = transaction;
            this.Config = creator.Config;
            this.creator = creator;
        }

        /// <summary>
        /// Creates an instance using a transaction.
        /// </summary>
        /// <param name="transaction"></param>
        /// <remarks>In this case, <see cref="this.Management"/> won't be considered for managing the logic; the original manager will remain open until it's manually closed.</remarks>
        /// <exception cref="ArgumentNullException"></exception>
        public ConnectionManager(QueryConfig config, DbTransaction transaction) : this(config, transaction?.Connection)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this._management = ConnectionManagement.LeaveOpen;
        }

        /// <summary>
        /// Creates an instance using a manager.
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConnectionManager(QueryConfig config, DbConnection connection)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.CommandTimeout = config.CommandTimeout;
            this.Config = config;
        }
        #endregion

        /// <summary>
        /// Attempt to close the connection using the reason "Operation completed".
        /// </summary>
        public void CloseByEndOperation()
        {
            if (this.Management == ConnectionManagement.CloseOnEndOperation && this.CanClose)
                this.Connection.Close();
        }

        public ConnectionManager Clone()
        {
            if (this.creator != null && this.Transaction != null)
                return new ConnectionManager(this.creator, this.Transaction)
                { _management = this._management };

            if (this.creator != null)
                return new ConnectionManager(this.creator)
                { CommandTimeout = this.CommandTimeout, _management = this._management };

            if (this.Transaction != null)
                return new ConnectionManager(this.Config, this.Transaction)
                { CommandTimeout = this.CommandTimeout, _management = this._management };

            return new ConnectionManager(this.Config, this.Connection)
            { CommandTimeout = this.CommandTimeout, _management = this._management };
        }

        /// <summary>
        /// Create a new ConnectionManager with a transaction from the existing connection (if the instance loads a <see cref="ConnectionCreator"/>, a new connection will be retrieved from it).
        /// </summary>
        /// <remarks>The commit will be automatically performed when calling <see cref="Dispose()"/> unless a <see cref="Commit"/> or <see cref="Rollback"/> has been called before.</remarks>
        public ConnectionManager BeginTransaction()
        {
            if (this.Transaction != null)
                throw new InvalidOperationException("There is already an open transaction.");

            if (this.creator != null)
                return new ConnectionManager(this.creator, this.creator.GetConnection().OpenIfNeeded().BeginTransaction())
                { _management = this._management };

            return new ConnectionManager(Config, this.Connection.OpenIfNeeded().BeginTransaction())
            { CommandTimeout = this.CommandTimeout, _management = this._management };
        }

        #region Transaction
        /// <summary>
        /// If there is a transaction, commit the database transaction.
        /// </summary>
        /// <returns></returns>
        private bool Commit()
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
        /// Executes a database transaction.
        /// </summary>
        public static void ExecuteTransaction(TransactionCall call)
        {
            if (!(ConnectionCreator.Default is ConnectionCreator creator))
                throw new InvalidOperationException($"It's not possible to start a transaction without setting a value for {nameof(ConnectionCreator)}.{nameof(ConnectionCreator.Default)}.");

            var manager = new ConnectionManager(creator, creator.GetConnection().OpenIfNeeded().BeginTransaction());

            try
            {
                call(manager);
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
        /// Executes a database transaction and returns a value.
        /// </summary>
        public static T ExecuteTransaction<T>(TransactionCall<T> func)
        {
            T value = default;
            ExecuteTransaction((transaction) => value = func(transaction));
            return value;
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (this.Transaction != null)
            {
                if (!this.isMyTransaction)
                    return;

                try { this.Commit(); } catch { }
                try { this.Transaction.Dispose(); } catch { }
            }

            if (this.Management == ConnectionManagement.LeaveOpen || this.Connection is null)
                return;

            if (this.creator != null) this.creator.SafeDisposeConnection(this.Connection);
            else this.Connection.Close();
        }

        ~ConnectionManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
