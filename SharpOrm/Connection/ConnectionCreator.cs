using SharpOrm.Builder;
using System;
using System.Data.Common;

namespace SharpOrm.Connection
{
    public abstract class ConnectionCreator : IDisposable
    {
        private bool _disposed;
        public bool Disposed => this._disposed;

        public static ConnectionCreator Default { get; set; }

        /// <summary>
        /// IQueryConfig defaults to "Query". The default object is "DefaultQueryConfig"
        /// </summary>
        public abstract IQueryConfig Config { get; }

        public abstract DbConnection GetConnection();

        public abstract void SafeDisposeConnection(DbConnection connection);

        #region Transaction
        public static void ExecuteTransaction(TransactionCall call)
        {
            DbConnection connection = Default.GetConnection();
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
                Default.SafeDisposeConnection(connection);
            }
        }

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
            if (_disposed)
                return;

            _disposed = true;
        }

        ~ConnectionCreator()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (this._disposed)
                throw new ObjectDisposedException(GetType().FullName);

            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
