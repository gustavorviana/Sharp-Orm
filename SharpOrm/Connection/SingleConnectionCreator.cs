using SharpOrm.Builder;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpOrm.Connection
{
    /// <summary>
    /// A generic version of <see cref="SingleConnectionCreator"/> that uses a specific <see cref="DbConnection"/> type.
    /// </summary>
    /// <typeparam name="T">A type of <see cref="DbConnection"/> that has a parameterless constructor.</typeparam>
    public class SingleConnectionCreator<T> : SingleConnectionCreator where T : DbConnection, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator{T}"/> class.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public SingleConnectionCreator(QueryConfig config, string connectionString) : base(typeof(T), config, connectionString, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator{T}"/> class with a configurator.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configurator">The connection configurator to apply additional settings.</param>
        public SingleConnectionCreator(QueryConfig config, string connectionString, IConnectionConfigurator configurator) : base(typeof(T), config, connectionString, configurator)
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
        /// Creates a clone of this <see cref="SingleConnectionCreator{T}"/> with the same configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="SingleConnectionCreator{T}"/> with identical settings.</returns>
        public override ConnectionCreator Clone()
        {
            return new SingleConnectionCreator<T>(Config, _connectionString, Configurator)
            {
                AutoOpenConnection = AutoOpenConnection,
                Management = Management
            };
        }
    }

    /// <summary>
    /// Provides a connection creator that maintains a single instance of <see cref="DbConnection"/>.
    /// </summary>
    public class SingleConnectionCreator : ConnectionCreator
    {
        /// <summary>
        /// The connection configurator used to apply additional settings to the connection.
        /// </summary>
        protected IConnectionConfigurator Configurator { get; }

        /// <summary>
        /// Synchronization object for thread-safe connection creation.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The database connection string.
        /// </summary>
        protected readonly string _connectionString;

        private readonly Type _dbConnectionType;
        private DbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator"/> class using <see cref="SqlConnection"/> as the connection type.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public SingleConnectionCreator(QueryConfig config, string connectionString) : this(typeof(SqlConnection), config, connectionString, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator"/> class using <see cref="SqlConnection"/> as the connection type with a configurator.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configurator">The connection configurator to apply additional settings.</param>
        public SingleConnectionCreator(QueryConfig config, string connectionString, IConnectionConfigurator configurator) : this(typeof(SqlConnection), config, connectionString, configurator)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator"/> class with a specific connection type.
        /// </summary>
        /// <param name="connectionType">The type of <see cref="DbConnection"/> to instantiate.</param>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public SingleConnectionCreator(Type connectionType, QueryConfig config, string connectionString) : this(connectionType, config, connectionString, null)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator"/> class with a specific connection type and configurator.
        /// </summary>
        /// <param name="connectionType">The type of <see cref="DbConnection"/> to instantiate.</param>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configurator">The connection configurator to apply additional settings.</param>
        public SingleConnectionCreator(Type connectionType, QueryConfig config, string connectionString, IConnectionConfigurator configurator)
        {
            ValidateConnectionType(connectionType);

            _dbConnectionType = connectionType;
            _connectionString = connectionString;
            Configurator = configurator;
            Config = config;
        }

        /// <summary>
        /// Returns a single instance of the configured <see cref="DbConnection"/>.
        /// </summary>
        /// <returns>A <see cref="DbConnection"/> instance.</returns>
        public override DbConnection GetConnection()
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (_connection == null)
                {
                    _connection = Activator.CreateInstance(_dbConnectionType) as DbConnection;
                    _connection.ConnectionString = _connectionString;
                    _connection.Disposed += OnConnectionDisposed;
                    Configurator?.Configure(_connection);
                }

                return AutoOpenConnection ? _connection.OpenIfNeeded() : _connection;
            }
        }

        private void OnConnectionDisposed(object sender, EventArgs e)
        {
            _connection = null;
        }

        /// <summary>
        /// Disposes the connection if it is the one managed by this instance.
        /// </summary>
        /// <param name="connection">The connection to potentially dispose.</param>
        public override void SafeDisposeConnection(DbConnection connection)
        {
            if (connection != null && _connection == connection)
                CloseConnection(false);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SingleConnectionCreator"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CloseConnection(true);

            if (disposing)
                DisposeUtils.SafeDispose(_connection, "SingleConnectionCreator.Dispose connection");

            _connection = null;
        }

        private void CloseConnection(bool forceClose)
        {
            if (_connection == null)
                return;

            DisposeUtils.SafeExecute(() =>
            {
                if (forceClose || (Management != ConnectionManagement.LeaveOpen && _connection.IsOpen()))
                    _connection.Close();
            }, "SingleConnectionCreator.CloseConnection");
        }

        /// <summary>
        /// Clones this <see cref="SingleConnectionCreator"/> with the same settings.
        /// </summary>
        /// <returns>A new instance of <see cref="SingleConnectionCreator"/> with the same configuration.</returns>
        public override ConnectionCreator Clone()
        {
            return new SingleConnectionCreator(_dbConnectionType, Config, _connectionString, Configurator)
            {
                AutoOpenConnection = AutoOpenConnection,
                Management = Management
            };
        }
    }
}