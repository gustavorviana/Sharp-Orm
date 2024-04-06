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

        public override QueryConfig Config { get; }

        public SingleConnectionCreator(QueryConfig config, string connectionString)
        {
            this._connectionString = connectionString;
            this.Config = config;
        }

        public override DbConnection GetConnection()
        {
            lock (this._lock)
            {
                this.ThrowIfDisposed();
                if (connection == null)
                {
                    connection = new T { ConnectionString = _connectionString };
                    connection.Disposed += OnConnectionDisposed;
                }

                return this.connection;
            }
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

            this.connection = null;
        }
    }
}