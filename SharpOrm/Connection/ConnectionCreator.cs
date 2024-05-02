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
        /// Gets or sets the default instance of the ConnectionCreator class.
        /// </summary>
        public static ConnectionCreator Default { get; set; }

        /// <summary>
        /// Configuration for queries.
        /// </summary>
        public abstract QueryConfig Config { get; }

        /// <summary>
        /// Gets a database connection.
        /// </summary>
        public abstract DbConnection GetConnection();

        /// <summary>
        /// Safely disposes a database connection.
        /// </summary>
        public abstract void SafeDisposeConnection(DbConnection connection);

        #region Transaction

        /// <summary>
        /// Executes a database transaction.
        /// </summary>
        [Obsolete("It will be removed in version 3.0.")]
        public static void ExecuteTransaction(TransactionCall call)
        {
            DbConnection connection = Default.GetConnection();
            connection.OpenIfNeeded();
            var transaction = connection.BeginTransaction();

            try
            {
                call(transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Close();
                Default.SafeDisposeConnection(connection);
            }
        }

        /// <summary>
        /// Executes a database transaction and returns a value.
        /// </summary>
        [Obsolete("It will be removed in version 3.0.")]
        public static T ExecuteTransaction<T>(TransactionCall<T> func)
        {
            T value = default;
            ExecuteTransaction((transaction) => value = func(transaction));
            return value;
        }
        #endregion

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
