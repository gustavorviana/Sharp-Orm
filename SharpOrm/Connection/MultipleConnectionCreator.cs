using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpOrm.Connection
{
    public class MultipleConnectionCreator : ConnectionCreator
    {
        private readonly List<DbConnection> connections = new List<DbConnection>();
        private readonly string _connectionString;

        public override IQueryConfig Config { get; }

        public MultipleConnectionCreator(IQueryConfig config, string connectionString)
        {
            this._connectionString = connectionString;
            this.Config = config;
        }

        public override DbConnection GetConnection()
        {
            if (this.Disposed)
                throw new ObjectDisposedException(this.GetType().FullName);

            var connection = new SqlConnection(this._connectionString);
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            connection.Disposed += OnConnectionDisposed;
            this.connections.Add(connection);
            return connection;
        }

        private void OnConnectionDisposed(object sender, EventArgs e)
        {
            if (!(sender is DbConnection con))
                return;

            con.Disposed -= OnConnectionDisposed;
            if (this.connections.Contains(con))
                this.connections.Remove(con);
        }

        public override void SafeDisposeConnection(DbConnection connection)
        {
            if (connection.State == ConnectionState.Open)
                connection.Close();

            connection.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            foreach (var con in this.connections)
                try { con.Dispose(); } catch { }

            this.connections.Clear();
        }
    }
}
