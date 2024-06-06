using SharpOrm.Builder;
using System;
using System.Data.Common;

namespace SharpOrm.Connection
{
    public abstract class ConnectionCreator : IDisposable
    {
        private bool _disposed;
        /// <summary>
        /// Indicates whether the ConnectionCreator object has been disposed.
        /// </summary>
        public bool Disposed => this._disposed;

        /// <summary>
        /// Open the connection by calling the <see cref="GetConnection"/> function.
        /// </summary>
        public bool AutoOpenConnection {  get; set; }

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