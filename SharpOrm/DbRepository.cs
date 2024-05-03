﻿using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm
{
    /// <summary>
    /// Represents an abstract base class for database repositories.
    /// </summary>
    public abstract class DbRepository : IDisposable
    {
        #region Fields/Properties
        private bool _externalTransaction = false;
        private bool _disposed;
        public bool Disposed => this._disposed;

        private ConnectionManager _transaction;
        protected ConnectionManager Transaction => this._transaction;

        private int? commandTimeout = null;
        protected int CommandTimeout
        {
            get => this.commandTimeout ?? this.Creator?.Config?.CommandTimeout ?? 30;
            set => this.commandTimeout = value;
        }

        /// <summary>
        /// Gets the default connection creator for the repository.
        /// </summary>
        protected virtual ConnectionCreator Creator => ConnectionCreator.Default;
        /// <summary>
        /// Indicates whether the repository has a parent transaction.
        /// </summary>
        protected bool HasParentTransaction => this._externalTransaction;

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
            this.SetTransaction(transaction is null ? null : new ConnectionManager(this.Creator.Config, transaction));
        }

        /// <summary>
        /// Set a transaction from a <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="manager"></param>
        public void SetTransaction(ConnectionManager manager)
        {
            this.ThrowIfDisposed();

            this._transaction = manager;
            this._externalTransaction = manager != null;
        }

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        public virtual void BeginTransaction()
        {
            this.ThrowIfDisposed();

            if (this.HasParentTransaction)
                return;

            if (this.HasTransaction)
                throw new DatabaseException(Messages.TransactionOpen);

            this._transaction = new ConnectionManager(this.Creator, true) { CommandTimeout = this.CommandTimeout, autoCommit = false };
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
            if (!this.HasTransaction)
                return;

            if (!this.HasParentTransaction)
            {
                var conn = this._transaction.Connection;
                this._transaction.Dispose();

                this.Creator.SafeDisposeConnection(conn);
            }

            this._transaction = null;
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
        protected virtual Query<T> Query<T>(DbName name) where T : new()
        {
            this.ThrowIfDisposed();

            if (this.Transaction != null)
                return new Query<T>(name, this.Transaction) { Token = this.Token, CommandTimeout = this.CommandTimeout };

            return new Query<T>(name, Creator) { Token = this.Token, CommandTimeout = this.CommandTimeout };
        }

        /// <summary>
        /// Creates a QueryConstructor for constructing custom queries.
        /// </summary>
        /// <param name="table">The name of the database table (optional).</param>
        /// <param name="alias">An alias for the table (optional).</param>
        /// <returns>A QueryConstructor for building custom queries.</returns>
        protected virtual QueryConstructor Constructor(string table = "", string alias = "")
        {
            return new QueryConstructor(this.Creator.Config, new DbName(table, alias));
        }

        /// <summary>
        /// Returns a <see cref="ConnectionManager"/> representing the connection of the current class.
        /// </summary>
        /// <returns></returns>
        protected virtual ConnectionManager GetConnectionManager()
        {
            if (this._transaction != null)
                return this.Transaction;

            return new ConnectionManager(this.Creator) { CommandTimeout = this.CommandTimeout };
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="query">The QueryConstructor used to create the command.</param>
        /// <returns></returns>
        protected int ExecuteNonQuery(QueryConstructor query)
        {
            using (var cmd = this.CreateCommand(query))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates a DbCommand from a QueryConstructor.
        /// </summary>
        /// <param name="query">The QueryConstructor used to create the command.</param>
        /// <returns>A DbCommand created from the QueryConstructor.</returns>
        protected DbCommand CreateCommand(QueryConstructor query)
        {
            return this.CreateCommand(query.ToString(), query.Parameters);
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

        protected internal DbCommand CreateCommand(string query, params object[] args)
        {
            return this.CreateCommand().SetQuery(query, args);
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
            using (var cmd = this.CreateCommand(query, args))
                return cmd.ExecuteArrayScalar<T>(this.Creator.Config.Translation);
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
            var cmd = this.GetConnection().CreateCommand();
            if (open) cmd.Connection?.OpenIfNeeded();

            cmd.CommandTimeout = this.CommandTimeout;
            cmd.Transaction = this._transaction?.Transaction;
            cmd.Disposed += OnCommandDisposed;

            return cmd;
        }

        #endregion

        private void OnCommandDisposed(object sender, EventArgs e)
        {
            if (!(sender is DbCommand cmd))
                return;

            if (cmd.Transaction is null)
                try { this.Creator.SafeDisposeConnection(cmd.Connection); } catch { }
        }

        /// <summary>
        /// Retrieves a database connection, optionally creating a new one if required.
        /// </summary>
        /// <param name="ignoreTransaction">
        ///     A flag indicating whether to force the creation of a new connection.
        ///     If set to true, a new connection will be created even if there is an active transaction.
        ///     If set to false, the connection from the active transaction, if present, will be returned.
        /// </param>
        /// <returns>
        ///     A DbConnection object, either a newly created connection or the one from the active transaction,
        ///     based on the value of the 'ignoreTransaction' parameter.
        /// </returns>
        protected virtual DbConnection GetConnection(bool ignoreTransaction = false)
        {
            this.ThrowIfDisposed();

            if (!ignoreTransaction && this.HasTransaction)
                return this._transaction.Connection;

            return this.Creator.GetConnection();
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
            if (!disposing)
                return;

            this._transaction = null;

            if (!this.HasParentTransaction && this.HasTransaction)
                this.CommitTransaction();
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