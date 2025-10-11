using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm
{
    /// <summary>
    /// Represents an abstract base class for database repositories.
    /// </summary>
    public abstract class DbRepository : IDisposable
    {
        #region Fields/Properties
        private readonly object _lock = new object();
        private readonly RepositoryOptions _options;
        private bool _isExtTransact = false;
        private bool _disposed;
        public bool Disposed => _disposed;

        internal readonly WeakComponentsRef<ConnectionManager> _connections = new WeakComponentsRef<ConnectionManager>();

        /// <summary>
        /// Event that occurs when an error happens in the ConnectionManager.
        /// </summary>
        public event EventHandler<ConnectionExceptionEventArgs> OnError;

        /// <summary>
        /// Indicates whether changes should be automatically committed when <see cref="ConnectionManager"/> is disposed.
        /// </summary>
        [Obsolete("This property will be removed in version 4.x. Use Options.AutoCommit instead.")]
        protected bool AutoCommit => _options.AutoCommit;

        /// <summary>
        /// Gets the connection manager for the current transaction.
        /// </summary>
        protected ConnectionManager Transaction { get; private set; }

        /// <summary>
        /// Gets or sets the command timeout value for database commands.
        /// </summary>
        /// <value>
        /// The command timeout value in seconds. If not explicitly set, defaults to the command timeout in the configuration or 30 seconds.
        /// </value>
        [Obsolete("This property will be removed in version 4.x. Use Options.CommandTimeout instead.")]
        protected int CommandTimeout
        {
            get => _options.CommandTimeout;
            set => _options.CommandTimeout = value;
        }

        [Obsolete("This property will be removed in version 4.x. Use Options.Translation instead.")]
        protected TranslationRegistry Translation => _options.Translation;

        /// <summary>
        /// Gets the default connection creator for the repository.
        /// </summary>
        [Obsolete("This property will be removed in version 4.x. Use Options.ConnectionCreator instead.")]
        protected virtual ConnectionCreator Creator => _options.ConnectionCreator;

        /// <summary>
        /// Indicates whether the repository has a parent transaction.
        /// </summary>
        protected bool HasParentTransaction => _isExtTransact;

        /// <summary>
        /// Gets or sets the cancellation token for the repository.
        /// </summary>
        public CancellationToken Token { get; set; }

        protected IRepositoryOptions Options => _options;
        #endregion

        public DbRepository(bool forceSingleConnection = false):this(ConnectionCreator.Default, forceSingleConnection)
        {
        }

        public DbRepository(ConnectionCreator creator, bool forceSingleConnection = false)
        {
            var builder = new RepositoryOptionsBuilder(creator, forceSingleConnection);
            OnConfiguring(builder);
            _options = builder.Build();
        }

        protected virtual void OnConfiguring(IRepositoryOptionsBuilder optionsBuilder)
        {
        }

        #region Transactions

        /// <summary>
        /// Sets the transaction based on another Repository service.
        /// </summary>
        /// <param name="service">The Repository service to set the transaction from.</param>
        public void SetTransaction(DbRepository service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (service.Disposed)
                throw new ObjectDisposedException(nameof(service), "Cannot set transaction from a disposed repository.");

            SetTransaction(service.Transaction);
        }

        /// <summary>
        /// Sets the transaction explicitly with a DbTransaction.
        /// </summary>
        /// <param name="transaction">The DbTransaction to set.</param>
        public void SetTransaction(DbTransaction transaction)
        {
            SetTransaction(transaction is null ? null : new ConnectionManager(_options.ConnectionCreator.Config, transaction));
        }

        /// <summary>
        /// Set a transaction from a <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="manager"></param>
        public void SetTransaction(ConnectionManager manager)
        {
            ThrowIfDisposed();

            Transaction = manager;
            _isExtTransact = manager != null;
        }

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        public virtual Task BeginTransactionAsync()
        {
            return TaskUtils.Async(BeginTransaction);
        }


        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        public virtual void BeginTransaction()
        {
            ThrowIfDisposed();

            if (HasParentTransaction)
                return;

            if (Transaction != null)
                throw new DatabaseException(Messages.TransactionOpen);

            Transaction = new ConnectionManager(_options.ConnectionCreator, true) { CommandTimeout = _options.CommandTimeout, _autoCommit = _options.AutoCommit };
        }

        /// <summary>
        /// Asynchronously commits the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task CommitTransactionAsync()
        {
            return TaskUtils.Async(CommitTransaction);
        }

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        public virtual void CommitTransaction()
        {
            if (HasParentTransaction)
                return;

            if (Transaction is null)
                throw new DatabaseException(Messages.TransactionNotOpen);

            Transaction.Commit();
            ClearTransaction();
        }

        /// <summary>
        /// Asynchronously rolls back the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task RollbackTransactionAsync()
        {
            return TaskUtils.Async(RollbackTransaction);
        }

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        public virtual void RollbackTransaction()
        {
            if (HasParentTransaction)
                return;

            if (Transaction is null)
                throw new DatabaseException(Messages.TransactionNotOpen);

            Transaction.Rollback();
            ClearTransaction();
        }

        /// <summary>
        /// Clears the current transaction and related resources.
        /// </summary>
        protected virtual void ClearTransaction()
        {
            if (Transaction is null)
                return;

            if (!HasParentTransaction)
            {
                var conn = Transaction.Connection;
                Transaction.Dispose();

                _options.ConnectionCreator.SafeDisposeConnection(conn);
            }

            Transaction = null;
        }

        /// <summary>
        /// Runs a transaction with the specified callback action.
        /// </summary>
        /// <param name="callback">The callback action to execute within the transaction.</param>
        public void RunTransaction(Action callback)
        {
            if (Transaction != null)
            {
                callback();
                return;
            }

            try
            {
                BeginTransaction();
                callback();
                CommitTransaction();
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Runs a transaction with a callback function and returns a result.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="callback">The callback function to execute within the transaction.</param>
        /// <returns>The result of the transaction.</returns>
        public T RunTransaction<T>(Func<T> callback)
        {
            T result = default;
            this.RunTransaction(new Action(() => result = callback()));
            return result;
        }

        #endregion

        #region Query

        /// <summary>
        /// Creates a Query object for a specific database table.
        /// </summary>
        /// <param name="table">The name of the database table to query.</param>
        /// <returns>A Query object for the specified table.</returns>
        protected Query Query(string table)
        {
            return this.Query(new DbName(table));
        }

        /// <summary>
        /// Creates a Query object for a specific database table.
        /// </summary>
        /// <param name="name">The name of the database table to query.</param>
        /// <returns>A Query object for the specified table.</returns>
        protected virtual Query Query(DbName name)
        {
            ThrowIfDisposed();

            return new Query(name, GetManager()) { Token = Token };
        }

        /// <summary>
        /// Creates a Query object for a specific type with an alias.
        /// </summary>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <param name="alias">An alias for the type in the query.</param>
        /// <returns>A Query object for the specified type with an alias.</returns>
        protected Query<T> Query<T>(string alias = "")
        {
            return Query<T>(DbName.Of<T>(alias));
        }

        /// <summary>
        /// Creates a Query object for a specific type with a DbName.
        /// </summary>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <param name="name">The DbName representing the type.</param>
        /// <returns>A Query object for the specified type with a DbName.</returns>
        protected virtual Query<T> Query<T>(DbName name)
        {
            ThrowIfDisposed();

            return new Query<T>(name, GetManager()) { Token = Token, CommandTimeout = _options.CommandTimeout };
        }

        /// <summary>
        /// Creates a QueryBuilder for constructing custom queries.
        /// </summary>
        /// <param name="table">The name of the database table (optional).</param>
        /// <param name="alias">An alias for the table (optional).</param>
        /// <returns>A QueryBuilder for building custom queries.</returns>
        protected virtual QueryBuilder Constructor(string table = "", string alias = "")
        {
            return new QueryBuilder(_options.ConnectionCreator.Config, new DbName(table, alias, false));
        }

        /// <summary>
        /// Executes a non-query SQL statement asynchronously.
        /// </summary>
        /// <param name="query">The QueryBuilder used to create the command.</param>
        /// <param name="token">The cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous operation, with the number of rows affected.</returns>
        protected async Task<int> ExecuteNonQueryAsync(QueryBuilder query, CancellationToken token)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).AddCancellationToken(token).SetExpression(query.ToExpression()))
                return await builder.SetExpressionWithAffectedRowsAsync(query.ToExpression()) + await builder.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="query">The QueryBuilder used to create the command.</param>
        /// <returns></returns>
        protected int ExecuteNonQuery(QueryBuilder query)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).SetExpression(query.ToExpression()))
                return builder.SetExpressionWithAffectedRows(query.ToExpression()) + builder.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a non-query SQL statement asynchronously.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <param name="token">The cancellation token to observe.</param>
        /// <param name="args">The arguments to be used in the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, with the number of rows affected.</returns>
        protected async Task<int> ExecuteNonQueryAsync(string query, CancellationToken token, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).AddCancellationToken(token).SetExpression(query, args))
                return await builder.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <returns></returns>
        protected int ExecuteNonQuery(string query, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).SetExpression(query, args))
                return builder.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a non-query SQL statement asynchronously.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <param name="token">The cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous operation, with the number of rows affected.</returns>
        protected async Task<int> ExecuteNonQueryAsync(string query, CancellationToken token)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).AddCancellationToken(token).SetExpression(query))
                return await builder.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <returns></returns>
        protected int ExecuteNonQuery(string query)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).SetExpression(query))
                return builder.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <param name="query">The SQL query string.</param>
        /// <param name="token">The cancellation token to observe.</param>
        /// <param name="args">The arguments to be used in the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, with the first column of the first row in the result set.</returns>
        protected async Task<T> ExecuteScalarAsync<T>(string query, CancellationToken token, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).SetExpression(query, args))
                return await builder.ExecuteScalarAsync<T>();
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        protected T ExecuteScalar<T>(string query, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).SetExpression(query, args))
                return builder.ExecuteScalar<T>();
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <param name="query">The SQL query string.</param>
        /// <param name="token">The cancellation token to observe.</param>
        /// <param name="args">The arguments to be used in the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, with an array of the first column of all rows in the result set.</returns>
        protected async Task<T[]> ExecuteArrayScalarAsync<T>(string query, CancellationToken token, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).AddCancellationToken(token).SetExpression(query, args))
                return await builder.ExecuteArrayScalarAsync<T>();
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        protected T[] ExecuteArrayScalar<T>(string query, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).SetExpression(query, args))
                return builder.ExecuteArrayScalar<T>();
        }

        /// <summary>
        /// Executes the provided SQL query asynchronously and returns an array of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="sql">The SQL query string.</param>
        /// <param name="token">The cancellation token to observe.</param>
        /// <param name="args">The arguments to be used in the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, with an array of the specified type.</returns>
        protected Task<T[]> ExecuteArrayAsync<T>(string sql, CancellationToken token, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).AddCancellationToken(token).SetExpression(sql, args))
                return builder.ExecuteArrayScalarAsync<T>();
        }

        /// <summary>
        /// Executes the provided SQL query and returns an array of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="sql">The SQL query string.</param>
        /// <param name="args">The arguments to be used in the SQL query.</param>
        /// <returns>An array of the specified type.</returns>
        protected T[] ExecuteArray<T>(string sql, params object[] args)
        {
            return ExecuteEnumerable<T>(sql, args).ToArray();
        }

        /// <summary>
        /// Executes the provided SQL query and returns an enumerable collection of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to be returned. Must have a parameterless constructor.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="args">An array of parameters to be applied to the SQL query.</param>
        /// <returns>An enumerable collection of objects of type <typeparamref name="T"/>.</returns>
        protected IEnumerable<T> ExecuteEnumerable<T>(string sql, params object[] args)
        {
            using (var manager = GetManager())
            using (var builder = GetBuilder(manager).SetExpression(sql, args))
                foreach (var item in builder.ExecuteEnumerable<T>())
                    yield return item;
        }

        private CommandBuilder GetBuilder(ConnectionManager manager)
        {
            return new CommandBuilder(manager).AddCancellationToken(Token);
        }

        /// <summary>
        /// Creates a DbCommand from a QueryBuilder.
        /// </summary>
        /// <param name="query">The QueryBuilder used to create the command.</param>
        /// <returns>A DbCommand created from the QueryBuilder.</returns>
        [Obsolete("This method will be removed in version 4.0.")]
        protected DbCommand CreateCommand(QueryBuilder query)
        {
            return CreateCommand().SetExpression(query.ToExpression(true, true));
        }

        /// <summary>
        /// Creates a DbCommand from a query string.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <param name="args">SQL query args.</param>
        /// <returns>A DbCommand created from the query string.</returns>
        protected internal DbCommand CreateCommand(string query, params object[] args)
        {
            return CreateCommand().SetQuery(query, args);
        }

        /// <summary>
        /// Creates a DbCommand from a query string.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <returns>A DbCommand created from the query string.</returns>
        protected internal DbCommand CreateCommand(string query)
        {
            var cmd = CreateCommand();
            cmd.CommandText = query;
            return cmd;
        }

        /// <summary>
        /// Creates a DbCommand.
        /// </summary>
        /// <param name="open">Signals if the command should already start with the connection open.</param>
        /// <returns>A DbCommand created from the query string.</returns>
        protected virtual DbCommand CreateCommand(bool open = true)
        {
            var cmd = this.GetManager().CreateCommand();
            if (open) cmd.Connection?.OpenIfNeeded();
            cmd.SetCancellationToken(Token);

            cmd.CommandTimeout = _options.CommandTimeout;
            cmd.Disposed += OnCommandDisposed;

            return cmd;
        }

        #endregion

        private void OnCommandDisposed(object sender, EventArgs e)
        {
            if (!(sender is DbCommand cmd))
                return;

            cmd.Disposed -= this.OnCommandDisposed;

            if (cmd.Transaction is null)
                try { _options.ConnectionCreator.SafeDisposeConnection(cmd.Connection); } catch { }
        }

        /// <summary>
        /// Retrieve a new connection manager (if not for reuse, only one per instance).
        /// </summary>
        /// <param name="useActiveTransaction">Indicate whether the active transaction should be retrieved if it exists.</param>
        /// <returns></returns>
        protected virtual ConnectionManager GetManager(bool useActiveTransaction = true)
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                if (useActiveTransaction && Transaction != null)
                    return Transaction;

                return GetExistingManager() ?? GetNewManager();
            }
        }

        private ConnectionManager GetExistingManager()
        {
            if (_options.ForceSingleConnection && _connections.Count > 0)
            {
                _connections.RemoveNotAlive();

                if (_connections.FirstOrDefault() is ConnectionManager conn)
                    return conn;
            }

            return null;
        }

        private ConnectionManager GetNewManager()
        {
            var connection = new ConnectionManager(_options.ConnectionCreator)
            {
                CommandTimeout = _options.CommandTimeout
            };

            if (OnError != null)
                connection.OnError += OnError;

            if (_options.ConnectionCreator is SingleConnectionCreator && _connections.Count > 0)
                connection = connection.Clone(true, true);

            _connections.Add(connection);
            return connection;
        }

        /// <summary>
        /// Asynchronously inserts a value of type T into the database.
        /// </summary>
        /// <typeparam name="T">The type of the value to insert.</typeparam>
        /// <param name="value">The value to insert.</param>
        /// <param name="token">The cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous operation, with the number of rows affected.</returns>
        protected Task<int> InsertAsync<T>(T value, CancellationToken token)
        {
            using (var query = Query<T>())
                return query.InsertAsync(value, token);
        }

        /// <summary>
        /// Inserts a value of type T into the database.
        /// </summary>
        /// <typeparam name="T">The type of the value to insert.</typeparam>
        /// <param name="value">The value to insert.</param>
        /// <returns>The number of rows affected.</returns>
        protected int Insert<T>(T value)
        {
            using (var query = Query<T>())
                return query.Insert(value);
        }

        #region DbTable

        /// <summary>
        /// Creates a new temporary table based on a query.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="queryBase">Query used to create the temporary table.</param>
        /// <returns>A DbTable instance for the created temporary table.</returns>
        protected DbTable CreateTempTable(string name, Query queryBase)
        {
            return DbTable.Create(name, true, queryBase, GetManager());
        }

        /// <summary>
        /// Creates a new temporary table based on an existing table.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="columns">Columns of the table to be used as the base.</param>
        /// <param name="basedTable">Name of the table to be used in the creation.</param>
        /// <returns>A DbTable instance for the created temporary table.</returns>
        protected DbTable CreateTempTable(string name, Column[] columns, string basedTable)
        {
            return DbTable.Create(name, true, columns, basedTable, GetManager());
        }

        /// <summary>
        /// Creates a temporary table based on the provided columns.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="columns">Columns that the table should contain.</param>
        /// <returns>A DbTable instance for the created temporary table.</returns>
        protected DbTable CreateTable(TableBuilder builder)
        {
            return DbTable.Create(builder.GetSchema(), GetManager());
        }

        /// <summary>
        /// Creates a temporary table based on a type.
        /// </summary>
        /// <typeparam name="T">The type to use as the base for the table.</typeparam>
        /// <returns>A DbTable instance for the created temporary table.</returns>
        protected DbTable CreateTempTable<T>()
        {
            return DbTable.Create<T>(true, _options.Translation, GetManager());
        }

        #endregion

        #region IDisposable

        ~DbRepository()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes of the Repository and associated resources.
        /// </summary>
        /// <param name="disposing">True if disposing of managed resources, false if finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
            if (disposing)
            {
                if (_options.AutoCommit && !HasParentTransaction && Transaction != null)
                    CommitTransaction();

                _connections.Dispose();
            }
            else
            {
                _connections.Clear();
            }

            Transaction = null;
        }

        /// <summary>
        /// Disposes of the Repository and associated resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        #endregion
    }
}