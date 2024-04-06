using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.Expressions;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace SharpOrm
{
    public class Query<T> : Query where T : new()
    {
        public static string TableName => TableInfo.GetNameOf(typeof(T));
        protected internal TableInfo TableInfo { get; }
        private LambdaColumn[] _fkToLoad = new LambdaColumn[0];

        #region Query
        public Query() : base(TableName)
        {
            TableInfo = new TableInfo(typeof(T));
            this.ApplyValidations();
        }

        public Query(string alias) : this(new DbName(TableName, alias))
        {
        }

        public Query(DbName name) : base(ConnectionCreator.Default, name)
        {
            TableInfo = new TableInfo(typeof(T));
            this.ApplyValidations();
        }

        public Query(ConnectionCreator creator, string alias = "") : base(creator, new DbName(TableName, alias))
        {
            TableInfo = new TableInfo(typeof(T));
            this.ApplyValidations();
        }

        public Query(ConnectionCreator creator, DbName table) : base(creator, table)
        {
            TableInfo = new TableInfo(typeof(T));
            this.ApplyValidations();
        }

        public Query(DbConnection connection, string alias = "") : this(connection, ConnectionCreator.Default?.Config, new DbName(TableName, alias))
        {
        }

        public Query(DbTransaction transaction, string alias = "") : this(transaction, ConnectionCreator.Default?.Config, new DbName(TableName, alias))
        {
        }

        public Query(QueryConfig config) : base(config, new DbName(TableName, null))
        {
            TableInfo = new TableInfo(typeof(T), config.Translation);
            this.ApplyValidations();
        }

        public Query(DbConnection connection, QueryConfig config, string alias = "") : this(connection, config, new DbName(TableName, alias))
        {
        }

        public Query(DbTransaction transaction, QueryConfig config, string alias = "") : this(transaction, config, new DbName(TableName, alias))
        {
        }

        public Query(DbConnection connection, QueryConfig config, DbName name) : base(connection, config, name)
        {
            TableInfo = new TableInfo(typeof(T), config.Translation);
            this.ApplyValidations();
        }

        public Query(DbTransaction transaction, QueryConfig config, DbName name) : base(transaction, config, name)
        {
            TableInfo = new TableInfo(typeof(T), config.Translation);
            this.ApplyValidations();
        }

        private void ApplyValidations()
        {
            this.ReturnsInsetionId = TableInfo.GetPrimaryKeys().Length > 0;
        }

        #endregion

        public Query<T> AddForeign(Expression<ColumnExpression<T>> call)
        {
            var cols = new ColumnExpressionVisitor().VisitColumn(call);
            TranslationUtils.AddToArray(ref this._fkToLoad, cols.Where(c => !this._fkToLoad.Any(fk => fk.Equals(c))).ToArray());
            return this;
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result.
        /// </summary>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number (One based).</param>
        /// <returns>A Pager<T> object for performing pagination on the query result.</returns>
        public Pager<T> Paginate(int peerPage, int currentPage)
        {
            return Pager<T>.FromBuilder(this, peerPage, currentPage);
        }

        public IEnumerable<T> GetEnumerable()
        {
            return base.GetEnumerable<T>();
        }

        /// <summary>
        /// Get first result.
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault()
        {
            int? lastLimit = this.Limit;
            this.Limit = 1;

            try
            {
                return this.GetEnumerable<T>().FirstOrDefault();
            }
            finally
            {
                this.Limit = lastLimit;
            }
        }

        public override IEnumerable<K> GetEnumerable<K>()
        {
            if (TranslationUtils.IsNullOrEmpty(_fkToLoad))
                return base.GetEnumerable<K>();

            try
            {
                using (var reader = new DbObjectReader(this.Config, this.ExecuteReader(), typeof(T)))
                {
                    reader.FkToLoad = this._fkToLoad;
                    reader.Token = this.Token;
                    reader.Connection = this.Connection;
                    reader.Transaction = this.Transaction;

                    return reader.ReadToEnd<K>();
                }
            }
            finally
            {
                this.SafeClose();
            }
        }

        /// <summary>
        /// Searches and returns the first occurrence of an object of type T that matches the values of the provided primary keys.
        /// </summary>
        /// <param name="primaryKeysValues">The values of the primary keys to search for.</param>
        /// <returns>The first occurrence of an object of type T that matches the provided primary keys.</returns>
        public T Find(params object[] primaryKeysValues)
        {
            using (var query = (Query<T>)this.Clone(false))
                return this.WherePk(primaryKeysValues).FirstOrDefault();
        }

        /// <summary>
        /// Add a clause to retrieve the items that have the primary key of the object.
        /// </summary>
        /// <param name="primaryKeysValues">Primary keys.</param>
        /// <returns></returns>
        public Query<T> WherePk(params object[] primaryKeysValues)
        {
            if (primaryKeysValues.Length == 0) throw new ArgumentNullException(nameof(primaryKeysValues));

            var pkCols = TableInfo.GetPrimaryKeys();
            ValidatePkVals(pkCols, primaryKeysValues);

            if (primaryKeysValues.Length == 1)
                return (Query<T>)this.Where(pkCols[0].Name, primaryKeysValues[0]);

            return (Query<T>)this.Where(query =>
            {
                for (var i = 0; i < primaryKeysValues.Length; i++)
                    query.Where(pkCols[i].Name, primaryKeysValues[i]);
            });
        }

        private static void ValidatePkVals(ColumnInfo[] keys, object[] pkValues)
        {
            if (keys.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (keys.Length != pkValues.Length)
                throw new ArgumentException(Messages.InsertValuesMismatch, nameof(pkValues));

            for (int i = 0; i < keys.Length; i++)
                if (!TranslationUtils.IsSimilar(keys[i].Type, pkValues[i]?.GetType()))
                    throw new InvalidCastException(Messages.InsertedTypeMismatch);
        }

        /// <summary>
        /// Get all available results.
        /// </summary>
        /// <returns></returns>
        public T[] Get()
        {
            return this.GetEnumerable<T>().ToArray();
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Id of row.</returns>
        public int Insert(T obj)
        {
            return this.Insert(this.GetCellsOf(obj, true));
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(params T[] objs)
        {
            return base.BulkInsert(objs.Select(obj => TableInfo.GetRow(obj, true, this.Config.LoadForeign)).ToArray());
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(IEnumerable<T> objs)
        {
            return base.BulkInsert(objs.Select(obj => TableInfo.GetRow(obj, true, this.Config.LoadForeign)));
        }

        /// <summary>
        /// Update table keys using object values.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int Update(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return base.Update(this.GetCellsOf(obj, false));
        }

        /// <summary>
        /// Update table keys using object values.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="calls">Calls to retrieve the properties that should be saved.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public int Update(T obj, params Expression<ColumnExpression<T>>[] calls)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (calls.Length == 0)
                throw new ArgumentNullException(nameof(calls));

            var props = PropertyExpressionVisitor.VisitProperties(calls).ToArray();
            return this.Update(this.GetCellsOf(obj, false, props));
        }

        /// <summary>
        /// Update table keys using object values.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="column1">Column to be updated</param>
        /// <param name="columns">Update table keys using object values..</param>
        /// <returns></returns>
        public int Update(T obj, params string[] columns)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (columns.Length == 0)
                throw new ArgumentNullException(nameof(columns));

            var toUpdate = SqlExtension.GetCellsByName(this.GetCellsOf(obj, false), columns).ToArray();
            if (toUpdate.Length == 0)
                throw new InvalidOperationException(Messages.ColumnsNotFound);

            return base.Update(toUpdate);
        }

        internal IEnumerable<Cell> GetCellsOf(T obj, bool readPk, string[] properties = null, bool needContains = true)
        {
            return TableInfo.GetObjCells(obj, readPk, this.Info.Config.LoadForeign, properties, needContains);
        }

        #region Join
        public Query Join<C>(string alias, string column1, string column2)
        {
            return this.Join<C>(alias, q => q.WhereColumn(column1, column2));
        }

        public Query Join<C>(string alias, string column1, string operation, string column2, string type = "INNER")
        {
            return this.Join<C>(alias, q => q.WhereColumn(column1, operation, column2), type);
        }

        public Query Join<C>(string alias, QueryCallback callback, string type = "INNER")
        {
            return base.Join(DbName.Of<C>(alias), callback, type); ;
        }
        #endregion

        public override Query Clone(bool withWhere)
        {
            Query<T> query = this.Transaction == null ?
                new Query<T>(this.Creator, this.Info.Alias) :
                 new Query<T>(this.Transaction, this.Info.Config, this.Info.Alias);

            query.Creator = this.Creator;

            if (withWhere)
                query.Info.LoadFrom(this.Info);

            this.OnClone(query);

            return query;
        }

        protected override void OnClone(Query cloned)
        {
            base.OnClone(cloned);

            if (!(cloned is Query<T> query))
                return;

            query._fkToLoad = (LambdaColumn[])this._fkToLoad.Clone();
        }
    }

    public class Query : QueryBase, ICloneable, IGrammarOptions
    {
        #region Properties
        public bool Distinct { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        internal string[] deleteJoins = null;
        /// <summary>
        /// Indicates whether the ID of the inserted row should be returned. (defaults true)
        /// </summary>
        public bool ReturnsInsetionId { get; set; } = true;

        public ConnectionCreator Creator { get; protected internal set; } = ConnectionCreator.Default;

        public object GrammarOptions { get; set; }
        protected QueryConfig Config => this.Info.Config;
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }
        public CancellationToken Token { get; set; }
        private readonly bool disposeConnection = true;
        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        public int CommandTimeout { get; set; }
        private OpenReader lastOpenReader = null;
        #endregion

        #region Query

        /// <summary>
        /// Creates a new instance of SharpOrm.Query using the default values ​​defined in SharpOrm.QueryDefaults.Default.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        public Query(string table) : this(ConnectionCreator.Default, table)
        {

        }

        public Query(ConnectionCreator creator, string table) : this(creator, new DbName(table))
        {
            this.Creator = creator;
        }

        public Query(ConnectionCreator creator, DbName table)
            : this(creator?.GetConnection() ?? throw new ArgumentNullException(nameof(creator), Messages.MissingCreator), creator.Config, table)
        {
            this.Creator = creator;
        }

        public Query(QueryConfig config, string table) : this(ConnectionCreator.Default?.GetConnection(), config, new DbName(table))
        {
        }

        public Query(QueryConfig config, DbName table) : this(ConnectionCreator.Default?.GetConnection(), config, table)
        {
        }

        public Query(DbConnection connection, string table) : this(connection, ConnectionCreator.Default.Config, new DbName(table))
        {
        }

        public Query(DbTransaction transaction, string table) : this(transaction, ConnectionCreator.Default.Config, new DbName(table))
        {

        }

        public Query(DbConnection connection, DbName table, bool disposeConnection = true) : this(connection, ConnectionCreator.Default.Config, table, disposeConnection)
        {
        }

        public Query(DbTransaction transaction, DbName table) : this(transaction, ConnectionCreator.Default.Config, table)
        {

        }

        public Query(DbConnection connection, QueryConfig config, string table, bool disposeConnection = true) : this(connection, config, new DbName(table), disposeConnection)
        {
        }

        public Query(DbConnection connection, QueryConfig config, DbName table, bool disposeConnection = true) : base(config, table)
        {
            this.disposeConnection = disposeConnection;
            if (connection == null)
                return;

            this.Connection = connection;
            this.CommandTimeout = config.CommandTimeout;
        }

        public Query(DbTransaction transaction, QueryConfig config, string table) : this(transaction, config, new DbName(table))
        {

        }

        public Query(DbTransaction transaction, QueryConfig config, DbName name) : base(config, name)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.Connection = transaction.Connection;
            this.CommandTimeout = config.CommandTimeout;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Select keys of table by name.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public Query Select(params string[] columnNames)
        {
            if (!columnNames.Any())
                throw new ArgumentNullException(nameof(columnNames), Messages.NoColumnsInserted);

            if (columnNames.Length == 1 && columnNames.First() == "*")
                return this.Select(Column.All);

            return this.Select(columnNames.Select(name => new Column(name)).ToArray());
        }

        /// <summary>
        /// Select column of table by Column object.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Query Select(params Column[] columns)
        {
            if (!columns.Any())
                throw new ArgumentNullException(nameof(columns), Messages.NoColumnsInserted);

            this.Info.Select = columns;

            return this;
        }

        #endregion

        #region Join

        public Query Join(string table, string column1, string column2)
        {
            return this.Join(table, q => q.WhereColumn(column1, column2));
        }

        public Query Join(string table, string column1, string operation, string column2, string type = "INNER")
        {
            return this.Join(table, q => q.WhereColumn(column1, operation, column2), type);
        }

        public Query Join(string table, QueryCallback callback, string type = "INNER", object grammarOptions = null)
        {
            return this.Join(new DbName(table), callback, type, grammarOptions);
        }

        public Query Join(DbName table, QueryCallback callback, string type = "INNER")
        {
            return this.Join(table, callback, type, this.GrammarOptions);
        }

        public Query Join(DbName table, QueryCallback callback, string type = "INNER", object grammarOptions = null)
        {
            JoinQuery join = new JoinQuery(this.Info.Config, table) { Type = type, GrammarOptions = grammarOptions };
            callback(join);
            this.Info.Joins.Add(join);
            return this;
        }

        #endregion

        #region GroupBy

        public Query GroupBy(params string[] columnNames)
        {
            return this.GroupBy(columnNames.Select(name => new Column(name)).ToArray());
        }

        public Query GroupBy(params Column[] columns)
        {
            this.Info.GroupsBy = columns;

            return this;
        }

        #endregion

        #region Having

        public Query Having(QueryCallback callback)
        {
            var qBase = new QueryBase(this.Config, this.Info.TableName);
            callback(qBase);
            this.Info.Having.Add(qBase.Info.Where);

            return this;
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(params string[] columns)
        {
            return this.OrderBy(SharpOrm.OrderBy.Asc, columns);
        }

        /// <summary>
        /// Applies descending sort.
        /// </summary>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderByDesc(params string[] columns)
        {
            return this.OrderBy(SharpOrm.OrderBy.Desc, columns);
        }

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="order">Field ordering.</param>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(OrderBy order, params string[] columns)
        {
            return this.OrderBy(order, columns.Select(c => new Column(c)).ToArray());
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param name="order">Field ordering.</param>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(OrderBy order, params Column[] columns)
        {
            return this.OrderBy(columns.Select(c => new ColumnOrder(c, order)).ToArray());
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public Query OrderBy(params ColumnOrder[] orders)
        {
            this.Info.Orders = orders.Where(x => x.Order != SharpOrm.OrderBy.None).ToArray();

            return this;
        }

        #endregion

        #region DML SQL commands

        public int Update(Dictionary<string, object> cells)
        {
            if (!cells.Any())
                throw new InvalidOperationException(Messages.NoColumnsInserted);

            return this.Update(cells.Select(x => new Cell(x.Key, x.Value)));
        }

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public int Update(params Cell[] cells)
        {
            if (!cells.Any())
                throw new InvalidOperationException(Messages.NoColumnsInserted);

            return this.Update((IEnumerable<Cell>)cells);
        }

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public int Update(IEnumerable<Cell> cells)
        {
            this.Token.ThrowIfCancellationRequested();
            this.CheckIsSafeOperation();

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                try
                {
                    int result = grammar.Update(cells).ExecuteNonQuery();
                    return result;
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        public int Insert(Dictionary<string, object> cells)
        {
            return this.Insert(cells.Select(x => new Cell(x.Key, x.Value)).ToArray());
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Id of row.</returns>
        public int Insert(params Cell[] cells)
        {
            if (cells.Length == 0)
                throw new InvalidOperationException(Messages.AtLeastOneColumnRequired);

            return this.Insert((IEnumerable<Cell>)cells);
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Id of row.</returns>
        public int Insert(IEnumerable<Cell> cells)
        {
            this.Token.ThrowIfCancellationRequested();
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                try
                {
                    object result = grammar.Insert(cells).ExecuteScalar();
                    return TranslationUtils.IsNumeric(result?.GetType()) ? Convert.ToInt32(result) : 0;
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        /// <summary>
        /// Insert a lot of values ​​using the result of a table (select command);
        /// </summary>
        /// <param name="query"></param>
        /// <param name="columnNames"></param>
        public int Insert(QueryBase query, params string[] columnNames)
        {
            this.Token.ThrowIfCancellationRequested();
            try
            {
                using (Grammar grammar = this.Info.Config.NewGrammar(this))
                    return grammar.InsertQuery(query, columnNames).ExecuteNonQuery();
            }
            finally
            {
                this.SafeClose();
            }
        }

        /// <summary>
        /// Insert a lot of values ​​using the result of a table (select command);
        /// </summary>
        /// <param name="query"></param>
        /// <param name="columnNames"></param>
        public void Insert(SqlExpression expression, params string[] columnNames)
        {
            this.Token.ThrowIfCancellationRequested();
            try
            {
                using (Grammar grammar = this.Info.Config.NewGrammar(this))
                    grammar.InsertExpression(expression, columnNames).ExecuteNonQuery();
            }
            finally
            {
                this.SafeClose();
            }
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(params Row[] rows)
        {
            return this.BulkInsert((ICollection<Row>)rows);
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(IEnumerable<Row> rows)
        {
            this.Token.ThrowIfCancellationRequested();
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                try
                {
                    return grammar.BulkInsert(rows).ExecuteNonQuery();
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        /// <summary>
        /// Removes rows from database
        /// </summary>
        /// <returns></returns>
        public int Delete()
        {
            this.Token.ThrowIfCancellationRequested();
            this.CheckIsSafeOperation();

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                try
                {
                    int result = grammar.Delete().ExecuteNonQuery();
                    return result;
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                this.Token.ThrowIfCancellationRequested();
                try
                {
                    object result = grammar.Count().ExecuteScalar();
                    return Convert.ToInt64(result);
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public long Count(string columnName)
        {
            return this.Count(new Column(columnName));
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public long Count(Column column)
        {
            this.Token.ThrowIfCancellationRequested();
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                try
                {
                    object result = grammar.Count(column).ExecuteScalar();
                    return Convert.ToInt64(result);
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        /// <summary>
        /// Returns all rows of the table
        /// </summary>
        /// <returns></returns>
        public Row[] ReadRows()
        {
            return this.GetEnumerable<Row>().ToArray();
        }

        /// <summary>
        /// Returns the first row of the table (if the table returns no value, null will be returned).
        /// </summary>
        /// <returns></returns>
        public Row FirstRow()
        {
            int? lastLimit = this.Limit;
            this.Limit = 1;

            try
            {
                return this.GetEnumerable<Row>().FirstOrDefault();
            }
            finally
            {
                this.Limit = lastLimit;
            }
        }

        public virtual IEnumerable<T> GetEnumerable<T>() where T : new()
        {
            this.Token.ThrowIfCancellationRequested();
            return new DbObjectEnumerable<T>(this);
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other keys are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public T[] ExecuteArrayScalar<T>()
        {
            this.Token.ThrowIfCancellationRequested();
            try
            {
                using (Grammar grammar = this.Info.Config.NewGrammar(this))
                using (DbCommand cmd = grammar.Select())
                    return cmd.ExecuteArrayScalar<T>(this.Config.Translation).ToArray();
            }
            finally
            {
                this.SafeClose();
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public T ExecuteScalar<T>()
        {
            this.Token.ThrowIfCancellationRequested();
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                try
                {
                    return this.Config.Translation.FromSql<T>(grammar.Select().ExecuteScalar());
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public object ExecuteScalar()
        {
            this.Token.ThrowIfCancellationRequested();
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            {
                try
                {
                    return this.Config.Translation.FromSql(grammar.Select().ExecuteScalar());
                }
                finally
                {
                    this.SafeClose();
                }
            }
        }

        /// <summary>
        /// Execute SQL Select command and return Collections.
        /// </summary>
        /// <returns></returns>
        public DbDataReader ExecuteReader()
        {
            if (this.lastOpenReader is OpenReader last)
                last.Dispose();

            return (this.lastOpenReader = new OpenReader(this.Info.Config.NewGrammar(this))).reader;
        }

        public Query JoinToDelete(params string[] join)
        {
            if (join == null || join.Length == 0)
                throw new ArgumentNullException(nameof(join));

            this.deleteJoins = join.Select(this.Config.ApplyNomenclature).ToArray();
            return this;
        }

        #endregion

        #region Clone and safety

        /// <summary>
        /// Clones the Query object with the parameters of "WHERE".
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this.Clone(true);
        }

        /// <summary>
        /// Clones the Query object.
        /// </summary>
        /// <param name="withWhere">Signals if the parameters of the "WHERE" should be copied.</param>
        /// <returns></returns>
        public virtual Query Clone(bool withWhere)
        {
            Query query = this.Transaction == null ? new Query(this.Creator.GetConnection(), this.Info.Config, this.Info.TableName) : new Query(this.Transaction, this.Info.Config, this.Info.TableName);
            query.Creator = this.Creator;

            if (withWhere)
                query.Info.LoadFrom(this.Info);

            this.OnClone(query);

            return query;
        }

        /// <summary>
        /// Throws an error if only operations with "WHERE" are allowed and there are none configured.
        /// </summary>
        protected void CheckIsSafeOperation()
        {
            if (this.Info.Config.OnlySafeModifications && this.Info.Where.Empty)
                throw new UnsafeDbOperation();
        }

        protected virtual void OnClone(Query cloned)
        {
            cloned.Distinct = this.Distinct;
            cloned.Limit = this.Limit;
            cloned.Offset = this.Offset;
        }

        #endregion

        protected internal virtual void SafeClose()
        {
            if (this.Transaction is null && this.Connection != null && this.Connection.State != System.Data.ConnectionState.Closed)
                this.Connection?.Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.lastOpenReader is OpenReader last)
                last.Dispose();

            if (this.disposeConnection)
                if (this.Creator is null && this.Transaction is null) this.Connection.Dispose();
                else this.Creator.SafeDisposeConnection(this.Connection);

            this.lastOpenReader = null;
        }

        public override string ToString()
        {
            using (var grammar = this.Info.Config.NewGrammar(this))
                return grammar.SelectSqlOnly();
        }

        private class OpenReader : IDisposable
        {
            private readonly Grammar grammar;
            public readonly DbDataReader reader;
            private bool _disposed;

            public OpenReader(Grammar grammar)
            {
                this.grammar = grammar;
                this.reader = grammar.Select().ExecuteReader();
            }

            public void Dispose()
            {
                if (this._disposed)
                    return;

                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~OpenReader()
            {
                this.Dispose(false);
            }

            protected void Dispose(bool disposing)
            {
                if (this._disposed)
                    return;

                this._disposed = true;

                if (!disposing)
                    return;

                try { ((IDisposable)this.reader).Dispose(); } catch { }
                try { this.grammar.Dispose(); } catch { }
            }
        }
    }
}
