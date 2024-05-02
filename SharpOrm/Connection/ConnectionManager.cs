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
        internal readonly ConnectionCreator creator;
        private bool disposed;
        private ConnectionManagement _management = ConnectionManagement.CloseOnDispose;

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
            if (this.creator != null)
                return new ConnectionManager(this.creator);

            if (this.Transaction != null)
                return new ConnectionManager(this.Config, this.Transaction);

            return new ConnectionManager(this.Config, this.Connection);
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (this.Management == ConnectionManagement.LeaveOpen || this.Connection is null || this.Transaction != null)
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
