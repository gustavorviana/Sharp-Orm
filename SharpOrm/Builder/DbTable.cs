using SharpOrm.Builder.Grammars;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a table in the database.
    /// </summary>
    public class DbTable : IDisposable
    {
        #region Fields/Properties
        internal readonly string[] _columnNames;
        private readonly TableGrammar _grammar;
        private bool _disposed;

        /// <summary>
        /// Table connection manager.
        /// </summary>
        public ConnectionManager Manager { get; }
        private readonly bool _isLocalManager;

        /// <summary>
        /// Table name.
        /// </summary>
        public DbName DbName => _grammar.Name;
        private bool _dropped;
        #endregion

        #region Creator and Constructors

        /// <summary>
        /// Create a new table based on a query.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="temporary">Indicate whether the table should be temporary or not.</param>
        /// <param name="queryBase">Query used to create the temporary table.</param>
        /// <param name="manager">Connection manager.</param>
        /// <returns></returns>
        [Obsolete("Use TableBuilder instead. This feature will be removed in version 4.0.")]
        public static DbTable Create(string name, bool temporary, Query queryBase, ConnectionManager manager = null)
        {
            return Create(new TableSchema(name, queryBase) { Temporary = temporary }, manager ?? queryBase.Manager);
        }

        /// <summary>
        /// Create a new table based on an existing table.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="temporary">Indicate whether the table should be temporary or not.</param>
        /// <param name="columns">Columns of the table to be used as the base.</param>
        /// <param name="basedTable">Name of the table to be used in the creation.</param>
        /// <param name="manager">Connection manager.</param>
        /// <returns></returns>
        [Obsolete("Use TableBuilder instead. This feature will be removed in version 4.0.")]
        public static DbTable Create(string name, bool temporary, Column[] columns, string basedTable, ConnectionManager manager = null)
        {
            var query = Query.ReadOnly(basedTable, manager?.Config).Select(columns);
            query.Limit = 0;

            return Create(new TableSchema(name, query) { Temporary = temporary }, manager);
        }

        /// <summary>
        /// Creates a table based on the provided columns.
        /// </summary>
        /// <param name="columns">Columns that the table should contain.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        [Obsolete("Use TableBuilder instead. This feature will be removed in version 4.0.")]
        public static DbTable Create(string name, bool temporary, TableColumnCollection columns, ConnectionManager manager = null)
        {
            return Create(new TableSchema(name, columns) { Temporary = temporary }, manager);
        }

        [Obsolete("Use TableBuilder instead. This feature will be removed in version 4.0.")]
        public static DbTable Create<T>(bool temporary, TranslationRegistry registry = null, ConnectionManager manager = null)
        {
            if (registry == null) registry = TranslationRegistry.Default;

            var table = registry.GetTable(typeof(T));
            var cols = new TableColumnCollection();
            cols.AddColumns(table.Columns.ToArray());

            return Create(new TableSchema(table.Name, cols) { Temporary = temporary }, manager);
        }

        /// <summary>
        /// Creates a table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable Create(ITableSchema schema, ConnectionManager manager = null)
        {
            if (schema.Columns.Count == 0 && !schema.Metadata.HasKey(Metadatas.BasedQuery))
                throw new InvalidOperationException("The schema does not contain any columns. At least one column is required.");

            bool isLocalManager = manager == null;
            if (manager is null)
                manager = new ConnectionManager() { Management = ConnectionManagement.CloseOnManagerDispose };

            ValidateConnectionManager(schema, manager);

            var clone = schema.Clone();

            var grammar = manager.Config.NewTableGrammar(clone);
            using (var cmd = manager.CreateCommand().SetExpression(grammar.Create()))
                cmd.ExecuteNonQuery();

            return new DbTable(grammar, manager, isLocalManager);
        }

        /// <summary>
        /// Opens an existing non temporary table;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="manager"></param>
        /// <exception cref="DatabaseException"></exception>
        public DbTable(string name, ConnectionManager manager = null)
        {
            _isLocalManager = manager == null;
            Manager = manager ?? new ConnectionManager() { Management = ConnectionManagement.CloseOnDispose };
            _grammar = Manager.Config.NewTableGrammar(new TableSchema(name) { Temporary = false });

            if (!Exists())
                throw new DatabaseException(string.Format(Messages.Table.TableNotFound, _grammar.Name));
        }

        private DbTable(TableGrammar grammar, ConnectionManager manager, bool isLocalManager = false)
        {
            Manager = manager;
            _grammar = grammar;
            _isLocalManager = isLocalManager;

            var schema = _grammar.Schema;
            _columnNames = schema.Metadata.GetOrDefault<string[]>(Metadatas.BasedColumns) ??
                schema.Columns?.Select(x => x.ColumnName)?.ToArray();
        }

        #endregion

        /// <summary>
        /// Retrieves a query object for the table.
        /// </summary>
        /// <returns></returns>
        public Query GetQuery()
        {
            var query = new Query(DbName, Manager);
            ConfigureColumns(query);
            return query;
        }

        /// <summary>
        /// Retrieves a query object for the table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Query<T> GetQuery<T>()
        {
            var query = new Query<T>(DbName, Manager);
            ConfigureColumns(query);
            return query;
        }

        private void ConfigureColumns(Query query)
        {
            if (_columnNames?.Length > 0)
                query.Select(_columnNames);
        }

        /// <summary>
        /// Inserts multiple values into the table in bulk.
        /// </summary>
        /// <typeparam name="T">The type of the values to insert.</typeparam>
        /// <param name="values">The array of values to insert.</param>
        public void BulkInsert<T>(ICollection<T> values)
        {
            using (var query = GetQuery<T>())
                query.BulkInsert(values, _columnNames);
        }

        /// <summary>
        /// Inserts a single value into the table.
        /// </summary>
        /// <typeparam name="T">The type of the value to insert.</typeparam>
        /// <param name="value">The value to insert.</param>
        public void Insert<T>(T value)
        {
            using (var query = GetQuery<T>())
                query.Insert(x => x.Add(value, _columnNames));
        }

        /// <summary>
        /// Asynchronously checks if the table exists.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with a boolean result indicating whether the table exists.</returns>
        public async Task<bool> ExistsAsync(CancellationToken token = default)
        {
            return await Manager.ExecuteScalarAsync<int>(_grammar.Exists(), token) > 0;
        }

        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Exists()
        {
            return Exists(_grammar, this.Manager);
        }

        /// <summary>
        /// Asynchronously deletes the table from the database.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DropAsync(CancellationToken token = default)
        {
            await Manager.ExecuteNonQueryAsync(_grammar.Drop(), token);
            _dropped = true;
        }

        /// <summary>
        /// Deletes the table from the database.
        /// </summary>
        public void Drop()
        {
            Manager.ExecuteNonQuery(_grammar.Drop());
            _dropped = true;
        }

        /// <summary>
        /// Asynchronously truncates the table.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task TruncateAsync(CancellationToken token = default)
        {
            await Manager.ExecuteNonQueryAsync(_grammar.Truncate(), token);
        }

        /// <summary>
        /// Truncates the table, removing all rows.
        /// </summary>
        public void Truncate()
        {
            Manager.ExecuteNonQuery(_grammar.Truncate());
        }

        public override string ToString()
        {
            if (_grammar.Schema.Temporary)
                return $"Temporary Table {this.DbName}";

            return $"Table {this.DbName}";
        }

        #region static's
        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Exists(string name, bool isTemp = false, ConnectionManager manager = null)
        {
            if (manager is null)
                throw new ArgumentNullException(nameof(manager));

            return Exists(
                manager.Config.NewTableGrammar(new TableSchema(name) { Temporary = isTemp }),
                manager
            );
        }

        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isTemp"></param>
        /// <param name="manager"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<bool> ExistsAsync(string name, bool isTemp = false, ConnectionManager manager = null, CancellationToken token = default)
        {
            if (manager is null)
                throw new ArgumentNullException(nameof(manager));

            var expression = manager.Config.NewTableGrammar(new TableSchema(name) { Temporary = isTemp }).Exists();
            return await manager.ExecuteScalarAsync<int>(expression, token) > 0;
        }

        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <param name="grammar"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static bool Exists(TableGrammar grammar, ConnectionManager manager)
        {
            return manager.ExecuteScalar<int>(grammar.Exists()) > 0;
        }

        private static void ValidateConnectionManager(ITableSchema schema, ConnectionManager manager)
        {
            if (schema.Temporary && manager.Management != ConnectionManagement.LeaveOpen && manager.Management != ConnectionManagement.CloseOnManagerDispose)
                throw new InvalidOperationException(Messages.Table.InvalidEmpTableConnection);
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                if (_grammar.Schema.Temporary && !_dropped)
                    Drop();
            }
            catch { }

            if (disposing && _isLocalManager)
                Manager.Dispose();
        }

        ~DbTable()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
