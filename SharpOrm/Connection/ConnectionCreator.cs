using SharpOrm.Builder;
using System;
using System.Data;
using System.Data.Common;

namespace SharpOrm.Connection
{
    /// <summary>
    /// Responsible for creating and configuring database connections. Doc: https://github.com/gustavorviana/Sharp-Orm/wiki/Connection-Creators
    /// </summary>
    public abstract class ConnectionCreator : IDisposable, ICloneable
    {
        private Version serverVersion;

        private bool _disposed;
        /// <summary>
        /// Indicates whether the ConnectionCreator object has been disposed.
        /// </summary>
        public bool Disposed => this._disposed;

        /// <summary>
        /// Indicates whether changes should be automatically committed when <see cref="ConnectionManager"/> is disposed.
        /// </summary>
        public bool AutoCommit { get; set; }

        /// <summary>
        /// Open the connection by calling the <see cref="GetConnection"/> function.
        /// </summary>
        public bool AutoOpenConnection { get; set; }

        /// <summary>
        /// Type of connection management.
        /// </summary>
        public ConnectionManagement Management { get; set; } = ConnectionManagement.CloseOnEndOperation;

        /// <summary>
        /// Gets or sets the default instance of the ConnectionCreator class.
        /// </summary>
        public static ConnectionCreator Default { get; set; }

        /// <summary>
        /// Gets the query configuration for the query build.
        /// </summary>
        public virtual QueryConfig Config { get; protected set; }

        /// <summary>
        /// Gets a database connection.
        /// </summary>
        public abstract DbConnection GetConnection();

        /// <summary>
        /// Safely disposes a database connection.
        /// </summary>
        public abstract void SafeDisposeConnection(DbConnection connection);

        /// <summary>
        /// Gets the server version.
        /// </summary>
        /// <param name="forceRefresh">If true, forces a refresh of the server version.</param>
        /// <returns>The server version.</returns>
        public Version GetServerVersion(bool forceRefresh = false)
        {
            if (serverVersion != null && !forceRefresh)
                return serverVersion;

            var conn = GetConnection();
            conn.OpenIfNeeded();
            try
            {
                return serverVersion = conn.GetVersion();
            }
            finally
            {
                SafeDisposeConnection(conn);
            }
        }

        /// <summary>
        /// Gets a ConnectionManager instance for managing database connections.
        /// </summary>
        /// <returns>A new instance of ConnectionManager.</returns>
        public ConnectionManager GetManager()
        {
            return new ConnectionManager(this);
        }

        /// <summary>
        /// Retrieves a <see cref="ConnectionManager"/> instance to manage database connections, initializing a transaction with the specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction.</param>
        /// <returns>A new instance of ConnectionManager.</returns>
        public ConnectionManager GetManager(IsolationLevel isolationLevel)
        {
            return new ConnectionManager(this, isolationLevel);
        }

        object ICloneable.Clone() => Clone();

        public abstract ConnectionCreator Clone();

        #region IDisposable

        /// <summary>
        /// Releases the unmanaged resources used by the ConnectionCreator object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        /// <summary>
        /// Destructor for the ConnectionCreator class.
        /// </summary>
        ~ConnectionCreator()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Releases the resources used by the ConnectionCreator object.
        /// </summary>
        public void Dispose()
        {
            this.ThrowIfDisposed();
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (this._disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        #endregion
    }
}