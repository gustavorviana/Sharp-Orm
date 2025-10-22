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
        public MultipleConnectionCreator(QueryConfig config, string connectionString) : base(typeof(T), config, connectionString, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator{T}"/> class with an optional configurator.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configurator">The optional connection configurator to apply additional settings.</param>
        public MultipleConnectionCreator(QueryConfig config, string connectionString, IConnectionConfigurator configurator)
                    : base(typeof(T), config, connectionString, configurator)
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

        /// <summary>
        /// Creates a clone of this <see cref="MultipleConnectionCreator{T}"/> with the same configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="MultipleConnectionCreator{T}"/> with identical settings.</returns>
        public override ConnectionCreator Clone()
        {
            return new MultipleConnectionCreator<T>(Config, _connectionString, Configurator)
            {
                AutoOpenConnection = AutoOpenConnection
            };
        }
    }

    /// <summary>
    /// Provides a connection creator that generates a new instance of <see cref="DbConnection"/> for each request.
    /// </summary>
    public class MultipleConnectionCreator : ConnectionCreator
    {
        private readonly WeakRefCollection<DbConnection> connections = new WeakRefCollection<DbConnection>();

        /// <summary>
        /// The connection configurator used to apply additional settings to the connections.
        /// </summary>
        protected IConnectionConfigurator Configurator { get; }

        /// <summary>
        /// The database connection string.
        /// </summary>
        protected readonly string _connectionString;

        private readonly Type _dbConnectionType;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator"/> class using <see cref="SqlConnection"/> as the connection type.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public MultipleConnectionCreator(QueryConfig config, string connectionString)
            : this(typeof(SqlConnection), config, connectionString, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator"/> class using <see cref="SqlConnection"/> as the connection type with a configurator.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configurator">The connection configurator to apply additional settings.</param>
        public MultipleConnectionCreator(QueryConfig config, string connectionString, IConnectionConfigurator configurator)
            : this(typeof(SqlConnection), config, connectionString, configurator)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator"/> class with a specific connection type.
        /// </summary>
        /// <param name="connectionType">The type of <see cref="DbConnection"/> to instantiate.</param>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public MultipleConnectionCreator(Type connectionType, QueryConfig config, string connectionString) : this(connectionType, config, connectionString, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleConnectionCreator"/> class with a specific connection type and configurator.
        /// </summary>
        /// <param name="connectionType">The type of <see cref="DbConnection"/> to instantiate.</param>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configurator">The connection configurator to apply additional settings.</param>
        public MultipleConnectionCreator(Type connectionType, QueryConfig config, string connectionString, IConnectionConfigurator configurator)
        {
            ValidateConnectionType(connectionType);

            _dbConnectionType = connectionType;
            _connectionString = connectionString;
            Config = config;
            Configurator = configurator;
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
            Configurator?.Configure(connection);

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

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MultipleConnectionCreator"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
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
            return new MultipleConnectionCreator(_dbConnectionType, Config, _connectionString, Configurator)
            {
                AutoOpenConnection = AutoOpenConnection
            };
        }
    }
}