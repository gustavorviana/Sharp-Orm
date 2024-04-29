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
        private readonly ConnectionCreator creator;
        private bool disposed;
        private ConnectionManagement _management = ConnectionManagement.CloseOnDispose;

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
        public DbTransaction Transaction { get; }
        public DbConnection Connection { get; }
        public int CommandTimeout { get; set; }
        #endregion

        /// <summary>
        /// Creates an instance using the default manager settings.
        /// </summary>
        public ConnectionManager() : this(ConnectionCreator.Default)
        {
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
        }

        /// <summary>
        /// Creates an instance using a transaction.
        /// </summary>
        /// <param name="transaction"></param>
        /// <remarks>In this case, <see cref="this.Management"/> won't be considered for managing the logic; the original manager will remain open until it's manually closed.</remarks>
        /// <exception cref="ArgumentNullException"></exception>
        public ConnectionManager(DbTransaction transaction)
        {
            this._management = ConnectionManagement.LeaveOpen;

            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.Connection = transaction.Connection;
        }

        /// <summary>
        /// Creates an instance using a manager.
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConnectionManager(DbConnection connection)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void CloseByEndOperation()
        {
            if (this.Management == ConnectionManagement.CloseOnEndOperation && this.CanClose())
                this.Connection.Close();
        }

        private bool CanClose()
        {
            return this.Transaction is null && this.Connection.State != System.Data.ConnectionState.Closed;
        }

        public ConnectionManager Clone()
        {
            if (this.creator != null)
                return new ConnectionManager(this.creator);

            if (this.Transaction != null)
                return new ConnectionManager(this.Transaction);

            return new ConnectionManager(this.Connection);
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
