using SharpOrm.Builder;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpOrm.Connection
{
    public class SingleConnectionCreator : SingleConnectionCreator<SqlConnection>
    {
        public SingleConnectionCreator(QueryConfig config, string connectionString) : base(config, connectionString)
        {
        }
    }

    public class SingleConnectionCreator<T> : ConnectionCreator where T : DbConnection, new()
    {
        private readonly object _lock = new object();
        private readonly string _connectionString;
        private DbConnection _connection;

        public SingleConnectionCreator(QueryConfig config, string connectionString)
        {
            _connectionString = connectionString;
            Config = config;
        }

        public override DbConnection GetConnection()
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (_connection == null)
                {
                    _connection = new T { ConnectionString = _connectionString };
                    _connection.Disposed += OnConnectionDisposed;
                }

                return AutoOpenConnection ? _connection.OpenIfNeeded() : _connection;
            }
        }

        private void OnConnectionDisposed(object sender, EventArgs e)
        {
            _connection = null;
        }

        public override void SafeDisposeConnection(DbConnection connection)
        {
            if (connection != null && _connection == connection)
                CloseConnection(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CloseConnection(true);

            try
            {
                if (disposing)
                    _connection?.Dispose();
            }
            catch
            { }

            _connection = null;
        }

        private void CloseConnection(bool forceClose)
        {
            if (_connection == null)
                return;

            try
            {
                if (forceClose || (Management != ConnectionManagement.LeaveOpen && _connection.IsOpen()))
                    _connection.Close();
            }
            catch (Exception) { }
        }

        public override ConnectionCreator Clone()
        {
            return new SingleConnectionCreator<T>(Config, _connectionString)
            {
                AutoOpenConnection = AutoOpenConnection,
                Management = Management
            };
        }
    }
}