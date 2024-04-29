using SharpOrm.Connection;
using System;
using System.Data;
using System.Data.Common;

namespace SharpOrm.Builder
{
    internal class TableBuilder : IDisposable
    {
        #region Fields/Properties
        private bool disposed;

        private TableGrammar grammar;

        public ConnectionManager ConnectionManager { get; }
        public DbConnection Connection => ConnectionManager.Connection;
        public DbName Name => this.grammar.Name;
        #endregion

        #region Constructor
        public TableBuilder(TableSchema schema) : this(schema, ConnectionCreator.Default.Config)
        {

        }

        public TableBuilder(TableSchema schema, QueryConfig config) : this(schema, config, new ConnectionManager())
        {
        }

        public TableBuilder(TableSchema schema, QueryConfig config, ConnectionManager manager)
        {
            this.ConnectionManager = manager;
            this.grammar = config.NewTableGrammar(schema);
            this.ConnectionManager.Management = ConnectionManagement.LeaveOpen;
        }
        #endregion

        public TableBuilder Create()
        {
            using (var cmd = GetCommand(this.grammar.Create()))
                cmd.ExecuteNonQuery();

            return this;
        }

        public bool Exists()
        {
            using (var cmd = GetCommand(this.grammar.Count()))
                return cmd.ExecuteScalar<int>() > 0;
        }

        public void DropTable()
        {
            try
            {
                using (var cmd = GetCommand(this.grammar.Drop()))
                    cmd.ExecuteNonQuery();
            }
            finally
            {
                if (this.ConnectionManager.Connection.State == ConnectionState.Open)
                    this.ConnectionManager.Connection.Close();
            }
        }

        public Query GetQuery()
        {
            return new Query(this.Name, grammar.Config, this.ConnectionManager);
        }

        public Query GetQuery<T>() where T : new()
        {
            return new Query<T>(this.Name, grammar.Config, this.ConnectionManager);
        }

        private DbCommand GetCommand(SqlExpression expression)
        {
            var cmd = this.ConnectionManager.Connection.OpenIfNeeded().CreateCommand();
            cmd.Transaction = this.ConnectionManager.Transaction;
            cmd.SetQuery(expression.ToString(), expression.Parameters);
            return cmd;
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            try
            {
                if (this.grammar.Schema.Temporary)
                    DropTable();
            }
            catch { }

            disposed = true;
        }

        ~TableBuilder()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (disposed) return;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
