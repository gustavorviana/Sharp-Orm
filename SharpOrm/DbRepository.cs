using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace SharpOrm
{
    /// <summary>
    /// Represents an abstract base class for database repositories.
    /// </summary>
    public abstract class DbRepository : IDisposable
    {
        #region Fields/Properties
        private readonly object _lock = new object();
        private readonly bool forceSingleConnection;
        private bool isExtTransact = false;
        private bool _disposed;
        public bool Disposed => this._disposed;

        internal readonly WeakComponentsRef<ConnectionManager> _connections = new WeakComponentsRef<ConnectionManager>();

        /// <summary>
        /// Gets the connection manager for the current transaction.
        /// </summary>
        protected ConnectionManager Transaction { get; private set; }

        private int? commandTimeout = null;
        /// <summary>
        /// Gets or sets the command timeout value for database commands.
        /// </summary>
        /// <value>
        /// The command timeout value in seconds. If not explicitly set, defaults to the command timeout in the configuration or 30 seconds.
        /// </value>
        protected int CommandTimeout
        {
            get => this.commandTimeout ?? this.Creator?.Config?.CommandTimeout ?? 30;
            set => this.commandTimeout = value;
        }

        protected TranslationRegistry Translation => this.Creator.Config.Translation;

        /// <summary>
        /// Gets the default connection creator for the repository.
        /// </summary>
        protected virtual ConnectionCreator Creator => ConnectionCreator.Default;

        /// <summary>
        /// Indicates whether the repository has a parent transaction.
        /// </summary>
        protected bool HasParentTransaction => this.isExtTransact;

        /// <summary>
        /// Gets or sets the cancellation token for the repository.
        /// </summary>
        public CancellationToken Token { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DbRepository"/> class.
        /// </summary>
        /// <param name="forceSingleConnection">A value indicating whether to force a single connection for the repository. Default is false.</param>
        public DbRepository(bool forceSingleConnection = false)
        {
            this.forceSingleConnection = forceSingleConnection;
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

            this.SetTransaction(service.Transaction);
        }

        /// <summary>
        /// Sets the transaction explicitly with a DbTransaction.
        /// </summary>
        /// <param name="transaction">The DbTransaction to set.</param>
        public void SetTransaction(DbTransaction transaction)
        {
            this.SetTransaction(transaction is null ? null : new ConnectionManager(this.Creator.Config, transaction));
        }

        /// <summary>
        /// Set a transaction from a <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="manager"></param>
        public void SetTransaction(ConnectionManager manager)
        {
            this.ThrowIfDisposed();

            this.Transaction = manager;
            this.isExtTransact = manager != null;
        }

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        public virtual void BeginTransaction()
        {
            this.ThrowIfDisposed();

            if (this.HasParentTransaction)
                return;

            if (this.Transaction != null)
                throw new DatabaseException(Messages.TransactionOpen);

            this.Transaction = new ConnectionManager(this.Creator, true) { CommandTimeout = this.CommandTimeout, autoCommit = false };
        }

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        public virtual void CommitTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (this.Transaction is null)
                throw new DatabaseException(Messages.TransactionNotOpen);

            this.Transaction.Commit();
            this.ClearTransaction();
        }

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        public virtual void RollbackTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (this.Transaction is null)
                throw new DatabaseException(Messages.TransactionNotOpen);

            this.Transaction.Rollback();
            this.ClearTransaction();
        }

        /// <summary>
        /// Clears the current transaction and related resources.
        /// </summary>
        protected virtual void ClearTransaction()
        {
            if (this.Transaction is null)
                return;

            if (!this.HasParentTransaction)
            {
                var conn = this.Transaction.Connection;
                this.Transaction.Dispose();

                this.Creator.SafeDisposeConnection(conn);
            }

            this.Transaction = null;
        }

        /// <summary>
        /// Runs a transaction with the specified callback action.
        /// </summary>
        /// <param name="callback">The callback action to execute within the transaction.</param>
        public void RunTransaction(Action callback)
        {
            if (this.Transaction != null)
            {
                callback();
                return;
            }

            try
            {
                this.BeginTransaction();
                callback();
                this.CommitTransaction();
            }
            catch
            {
                this.RollbackTransaction();
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
            this.ThrowIfDisposed();

            if (this.Transaction != null)
                return new Query(name, this.Transaction) { Token = this.Token, CommandTimeout = this.CommandTimeout };

            return new Query(name, Creator) { Token = this.Token };
        }

        /// <summary>
        /// Creates a Query object for a specific type with an alias.
        /// </summary>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <param name="alias">An alias for the type in the query.</param>
        /// <returns>A Query object for the specified type with an alias.</returns>
        protected Query<T> Query<T>(string alias = "")
        {
            return this.Query<T>(DbName.Of<T>(alias));
        }

        /// <summary>
        /// Creates a Query object for a specific type with a DbName.
        /// </summary>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <param name="name">The DbName representing the type.</param>
        /// <returns>A Query object for the specified type with a DbName.</returns>
        protected virtual Query<T> Query<T>(DbName name)
        {
            this.ThrowIfDisposed();

            if (this.Transaction != null)
                return new Query<T>(name, this.Transaction) { Token = this.Token, CommandTimeout = this.CommandTimeout };

            return new Query<T>(name, Creator) { Token = this.Token, CommandTimeout = this.CommandTimeout };
        }

        /// <summary>
        /// Creates a QueryBuilder for constructing custom queries.
        /// </summary>
        /// <param name="table">The name of the database table (optional).</param>
        /// <param name="alias">An alias for the table (optional).</param>
        /// <returns>A QueryBuilder for building custom queries.</returns>
        protected virtual QueryBuilder Constructor(string table = "", string alias = "")
        {
            return new QueryBuilder(this.Creator.Config, new DbName(table, alias, false));
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="query">The QueryBuilder used to create the command.</param>
        /// <returns></returns>
        protected int ExecuteNonQuery(QueryBuilder query)
        {
            using (var cmd = this.CreateCommand(query))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <returns></returns>
        protected int ExecuteNonQuery(string query, params object[] args)
        {
            using (var cmd = this.CreateCommand(query, args))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <returns></returns>
        protected int ExecuteNonQuery(string query)
        {
            using (var cmd = this.CreateCommand(query))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        protected T ExecuteScalar<T>(string query, params object[] args)
        {
            using (var cmd = this.CreateCommand(query, args))
                return cmd.ExecuteScalar<T>(this.Creator.Config.Translation);
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        protected IEnumerable<T> ExecuteArrayScalar<T>(string query, params object[] args)
        {
            return this.CreateCommand(query, args).ExecuteArrayScalar<T>(this.Creator.Config.Translation, this.Creator.Management);
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
            return CreateCommand(sql, args).ExecuteEnumerable<T>(null, this.Token, this.Creator.Management);
        }

        /// <summary>
        /// Creates a DbCommand from a QueryBuilder.
        /// </summary>
        /// <param name="query">The QueryBuilder used to create the command.</param>
        /// <returns>A DbCommand created from the QueryBuilder.</returns>
        protected DbCommand CreateCommand(QueryBuilder query)
        {
            return this.CreateCommand(query.ToString(), query.Parameters);
        }

        /// <summary>
        /// Creates a DbCommand from a query string.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <param name="args">SQL query args.</param>
        /// <returns>A DbCommand created from the query string.</returns>
        protected internal DbCommand CreateCommand(string query, params object[] args)
        {
            return this.CreateCommand().SetQuery(query, args);
        }

        /// <summary>
        /// Creates a DbCommand from a query string.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <returns>A DbCommand created from the query string.</returns>
        protected internal DbCommand CreateCommand(string query)
        {
            var cmd = this.CreateCommand();
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
            cmd.SetCancellationToken(this.Token);

            cmd.CommandTimeout = this.CommandTimeout;
            cmd.Disposed += OnCommandDisposed;

            return cmd;
        }

        #endregion

        private void OnCommandDisposed(object sender, EventArgs e)
        {
            if (!(sender is DbCommand cmd))
                return;

            cmd.Disposed += this.OnCommandDisposed;

            if (cmd.Transaction is null)
                try { this.Creator.SafeDisposeConnection(cmd.Connection); } catch { }
        }

        /// <summary>
        /// Retrieve a new connection manager (if not for reuse, only one per instance).
        /// </summary>
        /// <param name="useActiveTransaction">Indicate whether the active transaction should be retrieved if it exists.</param>
        /// <returns></returns>
        protected virtual ConnectionManager GetManager(bool useActiveTransaction = true)
        {
            this.ThrowIfDisposed();

            lock (this._lock)
            {
                if (useActiveTransaction && this.Transaction != null)
                    return this.Transaction;

                return GetExistingManager() ?? this.GetNewManager();
            }
        }

        private ConnectionManager GetExistingManager()
        {
            if (this.forceSingleConnection && this._connections.Count > 0)
            {
                this._connections.RemoveNotAlive();

                if (this._connections.FirstOrDefault() is ConnectionManager conn)
                    return conn;
            }

            return null;
        }

        private ConnectionManager GetNewManager()
        {
            var connection = new ConnectionManager(this.Creator)
            {
                CommandTimeout = this.CommandTimeout
            };
            _connections.Add(connection);
            return connection;
        }

        protected int Insert<T>(T value)
        {
            using (var query = this.Query<T>())
                return query.Insert(value);
        }

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
            if (this._disposed)
                return;

            this._disposed = true;
            if (disposing)
            {
                if (!this.HasParentTransaction && this.Transaction != null)
                    this.CommitTransaction();

                this._connections.Dispose();
            }
            else
            {
                this._connections.Clear();
            }

            this.Transaction = null;
        }

        /// <summary>
        /// Disposes of the Repository and associated resources.
        /// </summary>
        public void Dispose()
        {
            this.ThrowIfDisposed();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (this._disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        #endregion
    }
}