﻿using SharpOrm.Builder;
using SharpOrm.Collections;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpOrm.Connection
{
    /// <summary>
    /// Provides a multiple connection creator implementation using <see cref="SqlConnection"/>.
    /// </summary>
    public class MultipleConnectionCreator : MultipleConnectionCreator<SqlConnection>
    {
        public MultipleConnectionCreator(QueryConfig config, string connectionString) : base(config, connectionString)
        {
        }
    }

    /// <summary>
    /// Generic class for multiple connection creation.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="DbConnection"/> used for creating connections.</typeparam>
    public class MultipleConnectionCreator<T> : ConnectionCreator where T : DbConnection, new()
    {
        private readonly WeakComponentsRef<DbConnection> connections = new WeakComponentsRef<DbConnection>();
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator"/> class.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="connectionString">The connection string for the database.</param>
        public MultipleConnectionCreator(QueryConfig config, string connectionString)
        {
            _connectionString = connectionString;
            Config = config;
        }

        /// <summary>
        /// Gets a database connection.
        /// </summary>
        public override DbConnection GetConnection()
        {
            ThrowIfDisposed();
            var connection = new T { ConnectionString = _connectionString };

            connections.Add(connection);
            return AutoOpenConnection ? connection.OpenIfNeeded() : connection;
        }

        /// <summary>
        /// Safely disposes a database connection.
        /// </summary>
        public override void SafeDisposeConnection(DbConnection connection)
        {
            if (connection.State == ConnectionState.Open)
                connection.Close();

            connection.Dispose();
        }

        /// <summary>
        /// Releases the resources used by the <see cref="MultipleConnectionCreator"/> object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) connections.Dispose();
            else connections.Clear();
        }

        public override ConnectionCreator Clone()
        {
            return new MultipleConnectionCreator<T>(Config, _connectionString)
            {
                AutoOpenConnection = AutoOpenConnection
            };
        }
    }
}
