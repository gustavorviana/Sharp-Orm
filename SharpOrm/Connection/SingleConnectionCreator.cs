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
        private DbConnection connection;

        public SingleConnectionCreator(QueryConfig config, string connectionString)
        {
            this._connectionString = connectionString;
            this.Config = config;
        }

        public override DbConnection GetConnection()
        {
            this.ThrowIfDisposed();
            lock (this._lock)
            {
                if (connection == null)
                {
                    connection = new T { ConnectionString = _connectionString };
                    connection.Disposed += OnConnectionDisposed;
                }

                return this.AutoOpenConnection ? connection.OpenIfNeeded() : connection;
            }
        }

        private void OnConnectionDisposed(object sender, EventArgs e)
        {
            this.connection = null;
        }

        public override void SafeDisposeConnection(DbConnection connection)
        {
            if (connection != null && this.connection == connection)
                this.CloseConnection(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.CloseConnection(true);

            try
            {
                if (disposing)
                    this.connection?.Dispose();
            }
            catch
            { }

            this.connection = null;
        }

        private void CloseConnection(bool forceClose)
        {
            try
            {
                if (forceClose || (this.Management != ConnectionManagement.LeaveOpen && this.connection.IsOpen()))
                    this.connection.Close();
            }
            catch (Exception) { }
        }
    }
}