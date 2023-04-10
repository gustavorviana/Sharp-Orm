using SharpOrm.Builder;
using SharpOrm.Errors;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpOrm.Connection
{
    public class SingleConnectionCreator : SingleConnectionCreator<SqlConnection>
    {
        public SingleConnectionCreator(IQueryConfig config, string connectionString) : base(config, connectionString)
        {
        }
    }

    public class SingleConnectionCreator<T> : ConnectionCreator where T : DbConnection, new()
    {
        private readonly string _connectionString;
        private DbConnection connection;

        public override IQueryConfig Config { get; }

        public SingleConnectionCreator(IQueryConfig config, string connectionString)
        {
            this._connectionString = connectionString;
            this.Config = config;
        }

        public override DbConnection GetConnection()
        {
            if (connection == null)
            {
                connection = new T { ConnectionString = _connectionString };
                connection.Disposed += OnConnectionDisposed;
            }

            try
            {
                if (this.connection.State == System.Data.ConnectionState.Closed)
                    this.connection.Open();
            }
            catch (Exception ex)
            {
                throw new DbConnectionException(ex);
            }

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
}