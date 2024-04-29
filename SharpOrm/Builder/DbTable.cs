using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Data;
using System.Data.Common;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a table in the database.
    /// </summary>
    public class DbTable : IDisposable
    {
        #region Fields/Properties
        private TableGrammar grammar;
        private bool disposed;

        /// <summary>
        /// Table connection manager.
        /// </summary>
        public ConnectionManager Manager { get; }
        /// <summary>
        /// Table name.
        /// </summary>
        public DbName Name => grammar.Name;
        #endregion

        /// <summary>
        /// Creates a temporary table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <param name="config">QueryConfig used to create the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable CreateTemp(TableSchema schema, QueryConfig config = null, ConnectionManager manager = null)
        {
            schema.Temporary = true;
            return Create(schema, config, manager);
        }

        /// <summary>
        /// Creates a temporary table based on the provided columns.
        /// </summary>
        /// <param name="columns">Columns that the table should contain.</param>
        /// <param name="config">QueryConfig used to create the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable CreateTemp(string name, TableColumnCollection columns, QueryConfig config = null, ConnectionManager manager = null)
        {
            return Create(new TableSchema(name, columns), config, manager);
        }

        /// <summary>
        /// Creates a table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <param name="config">QueryConfig used to create the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable Create(TableSchema schema, QueryConfig config = null, ConnectionManager manager = null)
        {
            if (manager is null)
                manager = new ConnectionManager();

            if (config is null)
                config = manager.creator?.Config ?? ConnectionCreator.Default?.Config;

            var grammar = config.NewTableGrammar(schema.Clone());
            using (var cmd = GetCommand(manager, grammar.Create()))
                cmd.ExecuteNonQuery();

            return new DbTable(grammar, manager);
        }

        /// <summary>
        /// Opens an existing table.
        /// </summary>
        /// <param name="schema">Schema (Only name and if it's temporary) of the table.</param>
        /// <param name="config">QueryConfig used to open the table.</param>
        /// <param name="manager">Managed connection used to open the table.</param>
        /// <returns></returns>
        /// <exception cref="DatabaseException"></exception>
        public static DbTable Open(TableSchema schema, QueryConfig config = null, ConnectionManager manager = null)
        {
            if (manager is null)
                manager = new ConnectionManager();

            if (config is null)
                config = manager.creator?.Config ?? ConnectionCreator.Default?.Config;

            var grammar = config.NewTableGrammar(schema.Clone());
            if (!Exists(manager, schema, config))
                throw new DatabaseException($"The table '{grammar.Name}' was not found.");

            return new DbTable(grammar, manager);
        }

        private DbTable(TableGrammar grammar, ConnectionManager manager)
        {
            manager.Management = ConnectionManagement.LeaveOpen;
            Manager = manager;
            this.grammar = grammar;
        }

        /// <summary>
        /// Signals whether the table exists.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="schema"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Exists(ConnectionManager manager, TableSchema schema, QueryConfig config = null)
        {
            if (manager is null)
                throw new ArgumentNullException(nameof(manager));

            if (config is null)
                config = manager.creator?.Config ?? ConnectionCreator.Default?.Config;

            using (var cmd = GetCommand(manager, config.NewTableGrammar(schema).Count()))
                return cmd.ExecuteScalar<int>() > 0;
        }

        /// <summary>
        /// Deletes the table from the database.
        /// </summary>
        public void DropTable()
        {
            try
            {
                using (var cmd = GetCommand(Manager, grammar.Drop()))
                    cmd.ExecuteNonQuery();
            }
            finally
            {
                if (Manager.Connection.State == ConnectionState.Open)
                    Manager.Connection.Close();
            }
        }

        /// <summary>
        /// Retrieves a query object for the table.
        /// </summary>
        /// <returns></returns>
        public Query GetQuery()
        {
            return new Query(Name, grammar.Config, Manager);
        }

        /// <summary>
        /// Retrieves a query object for the table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Query GetQuery<T>() where T : new()
        {
            return new Query<T>(Name, grammar.Config, Manager);
        }

        private static DbCommand GetCommand(ConnectionManager manager, SqlExpression expression)
        {
            var cmd = manager.Connection.OpenIfNeeded().CreateCommand();
            cmd.Transaction = manager.Transaction;
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
                if (grammar.Schema.Temporary)
                    DropTable();
            }
            catch { }

            disposed = true;
        }

        ~DbTable()
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
