using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;

namespace SharpOrm
{
    /// <summary>
    /// Represents an abstract base class for database repositories.
    /// </summary>
    public abstract class DbRepository : IDisposable
    {
        #region Fields/Properties
        private readonly List<DbConnection> _connections = new List<DbConnection>();
        private readonly List<DbCommand> _commands = new List<DbCommand>();
        private readonly object _lock = new object();
        private bool _parentTransact = false;
        private bool _disposed;

        private DbTransaction _transaction;
        private DbConnection _transactionConn;

        /// <summary>
        /// Gets the default connection creator for the repository.
        /// </summary>
        protected virtual ConnectionCreator Creator => ConnectionCreator.Default;
        /// <summary>
        /// Indicates whether the repository has a parent transaction.
        /// </summary>
        protected bool HasParentTransaction => this._parentTransact;

        /// <summary>
        /// Indicates whether the repository has an active transaction.
        /// </summary>
        protected bool HasTransaction => this._transaction != null;

        /// <summary>
        /// Gets or sets the cancellation token for the repository.
        /// </summary>
        public CancellationToken Token { get; set; }
        #endregion

        #region Transactions

        /// <summary>
        /// Sets the transaction based on another Repository service.
        /// </summary>
        /// <param name="service">The Repository service to set the transaction from.</param>
        public void SetTransaction(DbRepository service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            this.SetTransaction(service._transaction);
        }

        /// <summary>
        /// Sets the transaction explicitly with a DbTransaction.
        /// </summary>
        /// <param name="transaction">The DbTransaction to set.</param>
        public void SetTransaction(DbTransaction transaction)
        {
            this._transaction = transaction;
            this._parentTransact = transaction != null;
        }

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        public virtual void BeginTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (this.HasTransaction)
                throw new DatabaseException(Messages.TransactionOpen);

            if (this._transactionConn == null)
                this._transactionConn = this.Creator.GetConnection();

            this._transaction = this._transactionConn.BeginTransaction();
        }

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        public virtual void CommitTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (!this.HasTransaction)
                throw new DatabaseException(Messages.TransactionNotOpen);

            this._transaction.Commit();
            this.ClearTransaction();
        }

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        public virtual void RollbackTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (!this.HasTransaction)
                throw new DatabaseException(Messages.TransactionNotOpen);

            this._transaction.Rollback();
            this.ClearTransaction();
        }

        /// <summary>
        /// Clears the current transaction and related resources.
        /// </summary>
        protected virtual void ClearTransaction()
        {
            if (this.HasTransaction && !this.HasParentTransaction)
                this._transaction.Dispose();

            this._transaction = null;
            if (this._transactionConn == null)
                return;

            this.Creator.SafeDisposeConnection(this._transactionConn);
            this._transactionConn = null;
        }

        /// <summary>
        /// Runs a transaction with the specified callback action.
        /// </summary>
        /// <param name="callback">The callback action to execute within the transaction.</param>
        public void RunTransaction(Action callback)
        {
            if (this.HasTransaction)
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
            if (this.HasTransaction)
                return new Query(this._transaction, this.Creator.Config, table) { Token = this.Token };

            return new Query(this.Creator, table) { Token = this.Token };
        }

        /// <summary>
        /// Creates a Query object for a specific type with an alias.
        /// </summary>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <param name="alias">An alias for the type in the query.</param>
        /// <returns>A Query object for the specified type with an alias.</returns>
        protected Query<T> Query<T>(string alias = "") where T : new()
        {
            return this.Query<T>(DbName.Of<T>(alias));
        }

        /// <summary>
        /// Creates a Query object for a specific type with a DbName.
        /// </summary>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <param name="name">The DbName representing the type.</param>
        /// <returns>A Query object for the specified type with a DbName.</returns>
        protected Query<T> Query<T>(DbName name) where T : new()
        {
            if (this.HasTransaction)
                return new Query<T>(this._transaction, this.Creator.Config, name) { Token = this.Token };

            return new Query<T>(this.Creator, name) { Token = this.Token };
        }

        /// <summary>
        /// Creates a QueryConstructor for constructing custom queries.
        /// </summary>
        /// <param name="table">The name of the database table (optional).</param>
        /// <param name="alias">An alias for the table (optional).</param>
        /// <returns>A QueryConstructor for building custom queries.</returns>
        protected virtual QueryConstructor Constructor(string table = "", string alias = "")
        {
            return new QueryConstructor(new QueryInfo(this.Creator.Config, new DbName(table, alias)));
        }

        /// <summary>
        /// Creates a DbCommand from a QueryConstructor.
        /// </summary>
        /// <param name="query">The QueryConstructor used to create the command.</param>
        /// <returns>A DbCommand created from the QueryConstructor.</returns>
        protected DbCommand CreateCommand(QueryConstructor query)
        {
            var qBuilder = new StringBuilder().AppendReplaced(query.ToString(), '?', index => $"@p{index - 1}");
            var cmd = this.CreateCommand(qBuilder.ToString());

            for (int i = 0; i < query.Parameters.Count; i++)
                cmd.AddCommand($"@p{i}", query.Parameters[i]);

            return cmd;
        }

        /// <summary>
        /// Creates a DbCommand from a query string.
        /// </summary>
        /// <param name="query">The SQL query string.</param>
        /// <returns>A DbCommand created from the query string.</returns>
        protected DbCommand CreateCommand(string query)
        {
            var cmd = this.GetConnection().CreateCommand();
            cmd.CommandText = query;
            cmd.Disposed += OnCommandDisposed;
            this._commands.Add(cmd);
            return cmd;
        }
        
        #endregion

        private void OnCommandDisposed(object sender, EventArgs e)
        {
            lock (_lock)
                if (sender is DbCommand cmd)
                    this._commands.Remove(cmd);
        }

        /// <summary>
        /// Retrieves a database connection, optionally creating a new one if required.
        /// </summary>
        /// <param name="forceNew">
        ///     A flag indicating whether to force the creation of a new connection.
        ///     If set to true, a new connection will be created even if there is an active transaction.
        ///     If set to false, the connection from the active transaction, if present, will be returned.
        /// </param>
        /// <returns>
        ///     A DbConnection object, either a newly created connection or the one from the active transaction,
        ///     based on the value of the 'forceNew' parameter.
        /// </returns>
        protected DbConnection GetConnection(bool forceNew = true)
        {
            if (!forceNew && this.HasTransaction)
                return this._transaction.Connection;

            var conn = this.Creator.GetConnection();
            this._connections.Add(conn);
            conn.Disposed += OnConnectionDisposed;
            return conn;
        }

        private void OnConnectionDisposed(object sender, EventArgs e)
        {
            lock (_lock)
                if (sender is DbConnection con)
                    this._connections.Remove(con);
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
        protected void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            if (!disposing)
            {
                this._transaction = null;
                this._transactionConn = null;

                this._commands.Clear();
                this._connections.Clear();
                return;
            }

            if (!this.HasParentTransaction && this.HasTransaction)
                this.CommitTransaction();

            foreach (var cmd in this._commands.ToArray())
                try { cmd.Dispose(); } catch { }

            foreach (var conn in this._connections.ToArray())
                try { conn.Dispose(); } catch { }
        }

        /// <summary>
        /// Disposes of the Repository and associated resources.
        /// </summary>
        public void Dispose()
        {
            if (this._disposed)
                throw new ObjectDisposedException(GetType().Name);

            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}