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
        public SingleConnectionCreator(QueryConfig config, string connectionString)
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
    /// Provides a connection creator that maintains a single instance of <see cref="DbConnection"/>.
    /// </summary>
    public class SingleConnectionCreator : ConnectionCreator
    {
        private readonly object _lock = new object();
        private readonly string _connectionString;
        private readonly Type _dbConnectionType;
        private DbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator"/> class using <see cref="SqlConnection"/> as the connection type.
        /// </summary>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public SingleConnectionCreator(QueryConfig config, string connectionString)
            : this(typeof(SqlConnection), config, connectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionCreator"/> class with a specific connection type.
        /// </summary>
        /// <param name="connectionType">The type of <see cref="DbConnection"/> to instantiate.</param>
        /// <param name="config">The query configuration to use.</param>
        /// <param name="connectionString">The database connection string.</param>
        public SingleConnectionCreator(Type connectionType, QueryConfig config, string connectionString)
        {
            ValidateConnectionType(connectionType);

            _dbConnectionType = connectionType;
            _connectionString = connectionString;
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

        /// <summary>
        /// Clones this <see cref="SingleConnectionCreator"/> with the same settings.
        /// </summary>
        /// <returns>A new instance of <see cref="SingleConnectionCreator"/> with the same configuration.</returns>
        public override ConnectionCreator Clone()
        {
            return new SingleConnectionCreator(_dbConnectionType, Config, _connectionString)
            {
                AutoOpenConnection = AutoOpenConnection,
                Management = Management
            };
        }
    }
}
