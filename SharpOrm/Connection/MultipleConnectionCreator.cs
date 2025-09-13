using SharpOrm.Builder;
using SharpOrm.Collections;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpOrm.Connection
{
    /// <summary>
    /// A generic version of <see cref="MultipleConnectionCreator"/> that uses a specific <see cref="DbConnection"/> type.
    /// </summary>
    /// <typeparam name="T">A type of <see cref="DbConnection"/> that has a parameterless constructor.</typeparam>
    public class MultipleConnectionCreator<T> : MultipleConnectionCreator where T : DbConnection, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator{T}"/> class.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public MultipleConnectionCreator(QueryConfig config, string connectionString)
            : base(typeof(T), config, connectionString)
        {
        }

        /// <summary>
        /// Returns a single instance of the configured <see cref="T"/>.
        /// </summary>
        /// <returns>A <see cref="T"/> instance.</returns>
        public new T GetConnection()
        {
            return (T)base.GetConnection();
        }
    }

    /// <summary>
    /// Provides a connection creator that generates a new instance of <see cref="DbConnection"/> for each request.
    /// </summary>
    public class MultipleConnectionCreator : ConnectionCreator
    {
        private readonly WeakComponentsRef<DbConnection> connections = new WeakComponentsRef<DbConnection>();
        private readonly string _connectionString;
        private readonly Type _dbConnectionType;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator"/> class using <see cref="SqlConnection"/> as the connection type.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public MultipleConnectionCreator(QueryConfig config, string connectionString)
            : this(typeof(SqlConnection), config, connectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator"/> class with a specific connection type.
        /// </summary>
        /// <param name="connectionType">The type of <see cref="DbConnection"/> to instantiate.</param>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public MultipleConnectionCreator(Type connectionType, QueryConfig config, string connectionString)
        {
            ValidateConnectionType(connectionType);

            _dbConnectionType = connectionType;
            _connectionString = connectionString;
            Config = config;
        }

        /// <summary>
        /// Creates and returns a new instance of <see cref="DbConnection"/>.
        /// </summary>
        /// <returns>A new <see cref="DbConnection"/> instance.</returns>
        public override DbConnection GetConnection()
        {
            ThrowIfDisposed();
            var connection = Activator.CreateInstance(_dbConnectionType) as DbConnection;
            connection.ConnectionString = _connectionString;

            connections.Add(connection);
            return AutoOpenConnection ? connection.OpenIfNeeded() : connection;
        }

        /// <summary>
        /// Safely disposes a connection by closing and disposing it.
        /// </summary>
        /// <param name="connection">The connection to dispose.</param>
        public override void SafeDisposeConnection(DbConnection connection)
        {
            if (connection.State == ConnectionState.Open)
                connection.Close();

            connection.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                connections.Dispose();
            else
                connections.Clear();
        }

        /// <summary>
        /// Clones this <see cref="MultipleConnectionCreator"/> with the same configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="MultipleConnectionCreator"/> with the same settings.</returns>
        public override ConnectionCreator Clone()
        {
            return new MultipleConnectionCreator(_dbConnectionType, Config, _connectionString)
            {
                AutoOpenConnection = AutoOpenConnection
            };
        }
    }
}
