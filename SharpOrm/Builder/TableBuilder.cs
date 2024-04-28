using SharpOrm.Connection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    internal class TableBuilder : IDisposable
    {
        #region Fields
        private bool disposed;

        private TableGrammar grammar;
        private TableSchema schema;
        public QueryConfig Config { get; }
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }
        public DbName Name => this.grammar.GetName(this.schema);
        #endregion

        #region Constructor
        public TableBuilder(TableSchema table) : this(table, ConnectionCreator.Default)
        {

        }

        public TableBuilder(TableSchema table, ConnectionCreator creator) : this(table, creator.Config, creator.GetConnection())
        {

        }

        public TableBuilder(TableSchema table, QueryConfig config, DbTransaction transaction) : this(table, config, transaction.Connection)
        {
            Transaction = transaction;
        }

        public TableBuilder(TableSchema table, QueryConfig config, DbConnection connection)
        {
            Config = config;
            Connection = connection;
            this.schema = table;
            this.grammar = this.Config.NewTableGrammar(new TempTableQuery(this.Config, new DbName(table.Name, "")));
        }
        #endregion

        public TableBuilder Create()
        {
            using (var cmd = GetCommand(this.grammar.Create(this.schema)))
                cmd.ExecuteNonQuery();

            return this;
        }

        public bool Exists()
        {
            if (this.schema == null)
                throw new InvalidOperationException("The table has not been created.");

            using (var cmd = GetCommand(this.grammar.Count(this.schema)))
                return cmd.ExecuteScalar<int>() > 0;
        }

        public void Drop()
        {
            try
            {
                using (var cmd = GetCommand(this.grammar.Drop(this.schema)))
                    cmd.ExecuteNonQuery();
            }
            finally
            {
                if (Connection.State == ConnectionState.Open)
                    Connection.Close();
            }
        }

        public Query GetQuery()
        {
            if (Transaction != null)
                return new Query(Transaction, Config, this.Name);

            return new Query(Connection, Config, this.Name, ConnectionManagement.LeaveOpen);
        }

        public Query GetQuery<T>() where T : new()
        {
            if (Transaction != null)
                return new Query<T>(Transaction, Config, this.Name);

            return new Query<T>(Connection, Config, this.Name, ConnectionManagement.LeaveOpen);
        }

        private DbCommand GetCommand(SqlExpression expression)
        {
            var cmd = Connection.OpenIfNeeded().CreateCommand();
            cmd.Transaction = Transaction;
            cmd.SetQuery(expression.ToString(), expression.Parameters);
            return cmd;
        }

        private class TempTableQuery : IReadonlyQueryInfo
        {
            public QueryConfig Config { get; set; }

            public DbName TableName { get; set; }

            public TempTableQuery(QueryConfig config, DbName tableName)
            {
                Config = config;
                TableName = tableName;
            }
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            try { Drop(); } catch { }

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
