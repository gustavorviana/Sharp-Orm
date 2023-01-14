using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace SharpOrm
{
    public sealed class ConnectionCreator<T> : ConnectionCreator where T : DbConnection, new()
    {
        private readonly string _connectionString;

        public ConnectionCreator(IQueryConfig config, string connectionString) : base(config)
        {
            _connectionString = connectionString;
        }

        protected override DbConnection CreateConnection()
        {
            return new T
            {
                ConnectionString = _connectionString
            };
        }
    }

    /// <summary>
    /// Default configuration for Query.
    /// </summary>
    public abstract class ConnectionCreator : IDisposable
    {
        private static readonly Dictionary<DbConnection, ConnectionCreator> _activeConnections = new Dictionary<DbConnection, ConnectionCreator>();
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

        #region Connection management
        /// <summary>
        /// Create new connection and open to a Query object. The default connection is null.
        /// </summary>
        /// <returns></returns>
        public DbConnection OpenConnection()
        {
            var connection = this.CreateConnection();
            connection.Disposed += Connection_Disposed;
            _activeConnections[connection] = this;
            connection.Open();
            return connection;
        }

        protected abstract DbConnection CreateConnection();

        private void Connection_Disposed(object sender, EventArgs e)
        {
            if (!(sender is DbConnection conn))
                return;

            conn.Disposed -= Connection_Disposed;
            if (_activeConnections.ContainsKey(conn))
                _activeConnections.Remove(conn);
        }

        internal static DbConnection NewFromType(DbConnection toClone)
        {
            return _activeConnections[toClone].OpenConnection();
        }
        #endregion

        #region Transaction
        public static void ExecuteTransaction(TransactionCall call)
        {
            DbConnection connection = Default.OpenConnection();
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
                connection.Dispose();
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
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var con in _activeConnections.Keys)
                    {
                        try
                        {
                            con.Dispose();
                        }
                        catch
                        { }
                    }
                }

                _disposed = true;
            }
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