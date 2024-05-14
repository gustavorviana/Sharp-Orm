﻿using SharpOrm.Builder;
using SharpOrm.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpOrm.Connection
{
    /// <summary>
    /// Provides a multiple connection creator implementation using SqlConnection.
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
    /// <typeparam name="T">The type of the DbConnection used for creating connections.</typeparam>
    public class MultipleConnectionCreator<T> : ConnectionCreator where T : DbConnection, new()
    {
        private readonly object _lock = new object();
        private readonly WeakComponentsRef<DbConnection> connections = new WeakComponentsRef<DbConnection>();
        private readonly string _connectionString;

        /// <summary>
        /// Gets the query configuration for the connection creator.
        /// </summary>
        public override QueryConfig Config { get; }

        /// <summary>
        /// Initializes a new instance of the MultipleConnectionCreator class.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="connectionString">The connection string for the database.</param>
        public MultipleConnectionCreator(QueryConfig config, string connectionString)
        {
            this._connectionString = connectionString;
            this.Config = config;
        }

        /// <summary>
        /// Gets a database connection.
        /// </summary>
        public override DbConnection GetConnection()
        {
            lock (this._lock)
            {
                this.ThrowIfDisposed();
                var connection = new T { ConnectionString = this._connectionString };

                this.connections.Add(connection);
                return connection;
            }
        }

        /// <summary>
        /// Safely disposes a database connection.
        /// </summary>
        public override void SafeDisposeConnection(DbConnection connection)
        {
            lock (this._lock)
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();

                connection.Dispose();
            }
        }

        /// <summary>
        /// Releases the resources used by the MultipleConnectionCreator object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                lock (this._lock)
                    this.connections.Dispose();
        }
    }
}
