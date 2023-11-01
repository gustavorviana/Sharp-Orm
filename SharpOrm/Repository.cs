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
    public abstract class Repository : IDisposable
    {
        #region Fields/Properties
        private readonly List<DbConnection> _connections = new List<DbConnection>();
        private readonly List<DbCommand> _commands = new List<DbCommand>();
        private readonly object _lock = new object();
        private bool _parentTransact = false;
        private bool _disposed;

        private DbTransaction _transaction;
        private DbConnection _transactConnection;

        protected virtual ConnectionCreator Creator => ConnectionCreator.Default;
        protected bool HasParentTransaction => this._parentTransact;
        protected bool HasTransaction => this._transaction != null;
        public CancellationToken Token { get; set; }
        #endregion

        #region Transactions

        public void SetTransaction(Repository service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            this.SetTransaction(service._transaction);
        }

        public void SetTransaction(DbTransaction transaction)
        {
            this._transaction = transaction;
            this._parentTransact = transaction != null;
        }

        public void BeginTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (this.HasTransaction)
                throw new DatabaseException("A transaction has already been opened.");

            if (this._transactConnection == null)
                this._transactConnection = this.Creator.GetConnection();

            this._transaction = this._transactConnection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (!this.HasTransaction)
                throw new DatabaseException("A transaction has not been opened.");

            this._transaction.Commit();
            this.ClearTransaction();
        }

        public void RollbackTransaction()
        {
            if (this.HasParentTransaction)
                return;

            if (!this.HasTransaction)
                throw new DatabaseException("A transaction has not been opened.");

            this._transaction.Rollback();
            this.ClearTransaction();
        }

        protected void ClearTransaction()
        {
            if (this.HasTransaction && !this.HasParentTransaction)
                this._transaction.Dispose();

            this._transaction = null;
            if (this._transactConnection == null)
                return;

            this.Creator.SafeDisposeConnection(this._transactConnection);
            this._transactConnection = null;
        }

        public void RunTransaction(Action callback)
        {
            if (this.HasTransaction)
                callback();

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

        public T RunTransaction<T>(Func<T> callback)
        {
            T result = default;
            this.RunTransaction(new Action(() => result = callback()));
            return result;
        }

        #endregion

        #region Query
        protected Query Query(string table)
        {
            if (this.HasTransaction)
                return new Query(this._transaction, table) { Token = this.Token };

            return new Query(this.Creator, table) { Token = this.Token };
        }

        protected Query<T> Query<T>(string alias = "") where T : new()
        {
            if (this.HasTransaction)
                return new Query<T>(this._transaction, alias) { Token = this.Token };

            return new Query<T>(this.Creator, alias) { Token = this.Token };
        }

        protected virtual QueryConstructor Constructor(string table = "", string alias = "")
        {
            return new QueryConstructor(new QueryInfo(this.Creator.Config, new DbName(table, alias)));
        }

        protected DbCommand CreateCommand(QueryConstructor query)
        {
            var qBuilder = new StringBuilder().AppendReplaced(query.ToString(), '?', index => $"@p{index - 1}");
            var cmd = this.CreateCommand(qBuilder.ToString());

            for (int i = 0; i < query.Parameters.Count; i++)
                cmd.AddCommand($"@p{i}", query.Parameters[i]);

            return cmd;
        }

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

        ~Repository()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            if (!disposing)
            {
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