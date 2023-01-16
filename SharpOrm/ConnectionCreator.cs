using SharpOrm.Builder;
using System;
using System.Data.Common;

namespace SharpOrm
{
    public class ConnectionCreator<T> : ConnectionCreator where T : DbConnection, new()
    {
        private readonly string _connectionString;
        private DbConnection connection;

        public ConnectionCreator(IQueryConfig config, string connectionString) : base(config)
        {
            _connectionString = connectionString;
        }

        public override DbConnection GetConnection()
        {
            if (connection == null)
            {
                connection = new T { ConnectionString = _connectionString };
                connection.Disposed += OnConnectionDisposed;
            }

            if (this.connection.State == System.Data.ConnectionState.Closed)
                this.connection.Open();

            return this.connection;
        }

        private void OnConnectionDisposed(object sender, EventArgs e)
        {
            this.connection = null;
        }

        public override void SafeDisposeConnection(DbConnection connection)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                this.connection?.Dispose();
        }
    }

    /// <summary>
    /// Default configuration for Query.
    /// </summary>
    public abstract class ConnectionCreator : IDisposable
    {
        private bool _disposed;

        public static ConnectionCreator Default { get; set; }

        /// <summary>
        /// IQueryConfig defaults to "Query". The default object is "DefaultQueryConfig"
        /// </summary>
        public IQueryConfig Config { get; }

        protected ConnectionCreator(IQueryConfig config)
        {
            this.Config = config;
        }

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