using SharpOrm.Builder.Expressions;
using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Tables;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm.Builder
{
    public class DbTable<T> : IDisposable
    {
        private bool _disposed;

        public DbTable OrmTable { get; private set; }

        public DbName Name => OrmTable.DbName;

        public DbTable(DbTable ormTable)
        {
            OrmTable = ormTable;
        }

        public static DbTable<T> CreateTempTable(Query query)
        {
            var builder = new TableBuilder(typeof(T).Name, true);
            builder.SetBasedQuery(query);

            return new DbTable<T>(DbTable.Create(builder.GetSchema(), query.Manager));
        }

        public Query<T> GetQuery(string alias)
        {
            return OrmTable.GetQuery<T>(alias);
        }

        public Query<T> GetQuery()
        {
            return OrmTable.GetQuery<T>();
        }

        public T[] ToArray()
        {
            using (var query = GetQuery())
            {
                return query.ToArray();
            }
        }

        public TValue[] ExecuteScalar<TValue>(Expression<ColumnExpression<T, TValue>> column)
        {
            using (var query = GetQuery())
            {
                query.SelectColumn(column);
                return query.ExecuteArrayScalar<TValue>();
            }
        }

        public DbTableValue<TValue> CreateTableValue<TValue>(Expression<ColumnExpression<T, TValue>> column)
        {
            using (var query = GetQuery())
            {
                query.SelectColumn(column);

                var processor = new ExpressionProcessor<T>(query, ExpressionConfig.All);
                return DbTableValue<TValue>.FromQuery(query, processor.ParseColumn(column).Name);
            }
        }

        public long Count()
        {
            using (var query = GetQuery())
            {
                return query.Count();
            }
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                OrmTable.Dispose();

            _disposed = true;
        }

        ~DbTable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// Represents a table in the database.
    /// </summary>
    public class DbTable : IDisposable
    {
        #region Fields/Properties
        private readonly TableGrammar _grammar;
        private string[] _columnNames;
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
        /// <returns></returns>
        [Obsolete("Use Create(ITableSchema, ConnectionManager) instead. This feature will be removed in version 4.0.")]
        public static DbTable Create(ITableSchema schema)
        {
            if (schema.Columns.Count == 0 && !schema.Metadata.HasKey(Metadatas.BasedQuery))
                throw new InvalidOperationException("The schema does not contain any columns. At least one column is required.");

            var manager = new ConnectionManager() { Management = ConnectionManagement.CloseOnManagerDispose };

            ValidateConnectionManager(schema.Temporary, manager.Management);

            var clone = schema.Clone();

            var grammar = manager.Config.NewTableGrammar(clone);
            using (var cmd = manager.CreateCommand().SetExpression(grammar.Create()))
                cmd.ExecuteNonQuery();

            return new DbTable(grammar, manager, true);
        }

        /// <summary>
        /// Creates a table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable Create(ITableSchema schema, ConnectionManager manager)
        {
            if (manager is null)
                throw new ArgumentNullException(nameof(manager));

            if (schema.Columns.Count == 0 && !schema.Metadata.HasKey(Metadatas.BasedQuery))
                throw new InvalidOperationException("The schema does not contain any columns. At least one column is required.");

            ValidateConnectionManager(schema.Temporary, manager.Management);

            var clone = schema.Clone();

            var grammar = manager.Config.NewTableGrammar(clone);
            using (var cmd = manager.CreateCommand().SetExpression(grammar.Create()))
                cmd.ExecuteNonQuery();

            return new DbTable(grammar, manager, false);
        }

        /// <summary>
        /// Opens an existing non temporary table;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="manager"></param>
        /// <exception cref="DatabaseException"></exception>
        [Obsolete("Use OpenIfExists instead. This feature will be removed in version 4.0.")]
        public DbTable(string name, ConnectionManager manager = null)
        {
            _isLocalManager = manager == null;
            Manager = manager ?? new ConnectionManager() { Management = ConnectionManagement.CloseOnDispose };
            _grammar = Manager.Config.NewTableGrammar(new TableSchema(name) { Temporary = false });

            if (!Exists())
                throw new DatabaseException(string.Format(Messages.Table.TableNotFound, _grammar.Name));
        }

        /// <summary>
        /// Opens an existing non temporary table if it exists.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="manager">Connection manager.</param>
        /// <returns>DbTable instance if the table exists, null otherwise.</returns>
        public static DbTable OpenIfExists(string name, ConnectionManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            var schema = new TableBuilder(name, temporary: false).GetSchema();
            var grammar = manager.Config.NewTableGrammar(schema);

            if (!Exists(grammar, manager))
                return null;

            return new DbTable(grammar, manager, false);
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
        /// <returns></returns>
        public Query GetQuery(string alias)
        {
            var query = new Query(new DbName(DbName.Name, alias), Manager);
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

        public Query<T> GetQuery<T>(string alias)
        {
            var query = new Query<T>(new DbName(DbName.Name, alias), Manager);
            ConfigureColumns(query);
            return query;
        }

        private void ConfigureColumns(Query query)
        {
            if (GetColumnNames()?.Length > 0)
                query.Select(GetColumnNames());
        }

        /// <summary>
        /// Inserts multiple values into the table in bulk.
        /// </summary>
        /// <typeparam name="T">The type of the values to insert.</typeparam>
        /// <param name="values">The array of values to insert.</param>
        public void BulkInsert<T>(ICollection<T> values)
        {
            using (var query = GetQuery<T>())
                query.BulkInsert(values, GetColumnNames());
        }

        /// <summary>
        /// Inserts a single value into the table.
        /// </summary>
        /// <typeparam name="T">The type of the value to insert.</typeparam>
        /// <param name="value">The value to insert.</param>
        public void Insert<T>(T value)
        {
            using (var query = GetQuery<T>())
                query.Insert(x => x.Add(value, GetColumnNames()));
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
            return Exists(_grammar, Manager);
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

        internal string[] GetColumnNames()
        {
            if (_columnNames?.Length > 0)
                return _columnNames;

            return _columnNames = GetColumns().Select(x => x.ColumnName).ToArray();
        }

        /// <summary>
        /// Retrieves metadata information about all columns in this table.
        /// </summary>
        /// <returns>An array of column metadata.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database does not support column inspection.</exception>
        public DbColumnInfo[] GetColumns()
        {
            var inspector = _grammar.CreateColumnInspector();
            if (inspector == null)
                throw new NotSupportedException($"Column inspection is not supported for {_grammar.Config.GetType().Name}.");

            using (var reader = Manager.ExecuteReader(inspector.GetColumnsQuery(), Manager.Config.Translation))
                return inspector.MapToColumnInfo(reader);
        }

        /// <summary>
        /// Asynchronously retrieves metadata information about all columns in this table.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with column metadata.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database does not support column inspection.</exception>
        public async Task<DbColumnInfo[]> GetColumnsAsync(CancellationToken token = default)
        {
            var inspector = _grammar.CreateColumnInspector();
            if (inspector == null)
                throw new NotSupportedException($"Column inspection is not supported for {_grammar.Config.GetType().Name}.");

            using (var reader = await Manager.ExecuteReaderAsync(inspector.GetColumnsQuery(), Manager.Config.Translation, token))
                return inspector.MapToColumnInfo(reader);
        }

        public override string ToString()
        {
            if (_grammar.Schema.Temporary)
                return $"Temporary Table {DbName}";

            return $"Table {DbName}";
        }

        #region static's
        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [Obsolete("This feature will be removed in version 4.0.")]
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
        [Obsolete("This feature will be removed in version 4.0.")]
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

        internal static void ValidateConnectionManager(bool temporary, ConnectionManagement management)
        {
            if (temporary && management != ConnectionManagement.LeaveOpen &&
                management != ConnectionManagement.CloseOnManagerDispose &&
                management != ConnectionManagement.DisposeOnManagerDispose)
                throw new InvalidOperationException(Messages.Table.InvalidTempTableConnection);
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
