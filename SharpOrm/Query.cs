using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace SharpOrm
{
    /// <summary>
    /// Class responsible for interacting with the data of a database table.
    /// </summary>
    /// <typeparam name="T">Type that should be used to interact with the table.</typeparam>
    public class Query<T> : Query
    {
        /// <summary>
        /// Table name in the database.
        /// </summary>
        public static string TableName => TableInfo.GetNameOf(typeof(T));
        protected internal TableInfo TableInfo { get; }
        private LambdaColumn[] _fkToLoad = new LambdaColumn[0];

        #region Query

        /// <summary>
        /// Creates a new instance of <see cref="Query"/> using the default values ​​defined in ConnectionCreator.Default.
        /// </summary>
        public Query() : this(new DbName(TableName), ConnectionCreator.Default)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="creator">Connection manager to be used.</param>
        public Query(ConnectionCreator creator) : this(new DbName(TableName, null), creator)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="manager">Connection manager to be used.</param>
        public Query(ConnectionManager manager) : this(new DbName(TableName, null), manager)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/> using the default values ​​defined in ConnectionCreator.Default.
        /// </summary>
        /// <param name="alias">Alias for the table.</param>
        public Query(string alias) : this(new DbName(TableName, alias))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="alias">Alias for the table.</param>
        /// <param name="creator">Connection manager to be used.</param>
        public Query(string alias, ConnectionCreator creator) : this(new DbName(TableName, alias), creator)
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="alias">Alias for the table.</param>
        /// <param name="manager">Connection manager to be used.</param>
        public Query(string alias, ConnectionManager manager) : this(new DbName(TableName, alias), manager)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/> using the default values ​​defined in ConnectionCreator.Default.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        public Query(DbName table) : this(table, ConnectionCreator.Default)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="creator">Connection manager to be used.</param>
        public Query(DbName table, ConnectionCreator creator) : this(table, new ConnectionManager(creator))
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="manager">Connection manager to be used.</param>
        public Query(DbName table, ConnectionManager manager) : base(table, manager)
        {
            TableInfo = manager.Config.Translation.GetTable(typeof(T));
            this.ApplyValidations();
        }

        private void ApplyValidations()
        {
            this.ReturnsInsetionId = !TableInfo.IsManualMap && TableInfo.GetPrimaryKeys().Length > 0;
        }

        #endregion

        /// <summary>
        /// Adds a foreign key to the query based on the specified column expression.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the query.</typeparam>
        /// <param name="call">An expression representing the column to be added as a foreign key.</param>
        /// <returns>The query with the added foreign key.</returns>
        public Query<T> AddForeign(Expression<ColumnExpression<T>> call)
        {
            var cols = new ColumnExpressionVisitor().VisitColumn(call);
            TranslationUtils.AddToArray(ref this._fkToLoad, cols.Where(c => !this._fkToLoad.Any(fk => fk.Equals(c))).ToArray());
            return this;
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result.
        /// </summary>
        /// <param table="peerPage">The number of items per page.</param>
        /// <param table="currentPage">The current page number (One based).</param>
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
            var enumerable = (DbCommandEnumerable<K>)base.GetEnumerable<K>();
            if (TranslationUtils.IsNullOrEmpty(_fkToLoad))
                return enumerable;

            try
            {
                FkLoaders fkLoaders = new FkLoaders(this.Manager, this._fkToLoad, this.Token);

                enumerable.fkQueue = fkLoaders;
                var list = enumerable.ToList();
                fkLoaders.LoadForeigns();

                return list;
            }
            finally
            {
                this.Manager?.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Searches and returns the first occurrence of an object of type T that matches the values of the provided primary keys.
        /// </summary>
        /// <param table="primaryKeysValues">The values of the primary keys to search for.</param>
        /// <returns>The first occurrence of an object of type T that matches the provided primary keys.</returns>
        public T Find(params object[] primaryKeysValues)
        {
            using (var query = (Query<T>)this.Clone(false))
                return this.WherePk(primaryKeysValues).FirstOrDefault();
        }

        /// <summary>
        /// AddRaws a clause to retrieve the items that have the primary key of the object.
        /// </summary>
        /// <param table="primaryKeysValues">Primary keys.</param>
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
        /// <param table="obj"></param>
        /// <returns>Id of row.</returns>
        public int Insert(T obj)
        {
            return this.Insert(this.GetCellsOf(obj, true, validate: true));
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param table="rows"></param>
        public int BulkInsert(params T[] objs)
        {
            return base.BulkInsert(objs.Select(ValidateAndConvert));
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param table="rows"></param>
        public int BulkInsert(IEnumerable<T> objs)
        {
            return base.BulkInsert(objs.Select(ValidateAndConvert));
        }

        private Row ValidateAndConvert(T obj)
        {
            return TableInfo.GetRow(obj, true, this.Config.LoadForeign, true);
        }

        /// <summary>
        /// Update table keys using object values.
        /// </summary>
        /// <param table="obj"></param>
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
        /// <param table="obj"></param>
        /// <param table="calls">Calls to retrieve the properties that should be saved.</param>
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
        /// <param table="obj"></param>
        /// <param table="column1">Column to be updated</param>
        /// <param table="columns">Update table keys using object values..</param>
        /// <returns></returns>
        public int Update(T obj, params string[] columns)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (columns.Length == 0)
                throw new ArgumentNullException(nameof(columns));

            var toUpdate = SqlExtension.GetCellsByName(this.GetCellsOf(obj, false, validate: true), columns).ToArray();
            if (toUpdate.Length == 0)
                throw new InvalidOperationException(Messages.ColumnsNotFound);

            return base.Update(toUpdate);
        }

        internal IEnumerable<Cell> GetCellsOf(T obj, bool readPk, string[] properties = null, bool needContains = true, bool validate = false)
        {
            return TableInfo.GetObjCells(obj, readPk, this.Info.Config.LoadForeign, properties, needContains, validate);
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
            Query<T> query = new Query<T>(this.Info.TableName, this.Manager);

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

    /// <summary>
    /// Class responsible for interacting with the data of a database table.
    /// </summary>
    public class Query : QueryBase, ICloneable, IGrammarOptions
    {
        #region Properties
        internal string[] deleteJoins = null;

        /// <summary>
        /// Indicate whether the database should return only distinct items.
        /// </summary>
        public bool Distinct { get; set; }
        /// <summary>
        /// Maximum number of items that the database can interact with in the execution.
        /// </summary>
        public int? Limit { get; set; }
        /// <summary>
        /// Number of items to be initially ignored by the database.
        /// </summary>
        /// <remarks>Example: If <see cref="Offset"/> is 3, the database will ignore the first 3 items in the execution.</remarks>
        public int? Offset { get; set; }
        /// <summary>
        /// Indicates whether the ID of the inserted row should be returned. (defaults true)
        /// </summary>
        public bool ReturnsInsetionId { get; set; } = true;

        /// <summary>
        /// Options for Grammar to create an SQL script.
        /// </summary>
        public object GrammarOptions { get; set; }
        /// <summary>
        /// Settings used to build the SQL command.
        /// </summary>
        public QueryConfig Config => this.Info.Config;
        /// <summary>
        /// Connection manager of the query.
        /// </summary>
        public ConnectionManager Manager { get; }

        /// <summary>
        /// Cancellation token for the commands to be executed in this query.
        /// </summary>
        public CancellationToken Token { get; set; }
        private int? _commandTimeout;
        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        public int CommandTimeout
        {
            get => this._commandTimeout ?? this.Manager.CommandTimeout;
            set => this._commandTimeout = value;
        }
        private OpenReader lastOpenReader = null;
        #endregion

        #region Query

        /// <summary>
        /// Creates a read-only query for the specified table.
        /// </summary>
        /// <param name="table">The name of the table.</param>
        /// <param name="config">The configuration for the query. If null, the default configuration is used.</param>
        /// <returns>A read-only query for the specified table.</returns>
        public static Query ReadOnly(string table, QueryConfig config = null)
        {
            return ReadOnly(new DbName(table), config);
        }

        /// <summary>
        /// Creates a read-only query for the specified table.
        /// </summary>
        /// <param name="table">The name of the table as a <see cref="DbName"/> object.</param>
        /// <param name="config">The configuration for the query. If null, the default configuration is used.</param>
        /// <returns>A read-only query for the specified table.</returns>
        public static Query ReadOnly(DbName table, QueryConfig config = null)
        {
            return new Query(table, config ?? ConnectionCreator.Default?.Config);
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/> using the default values ​​defined in ConnectionCreator.Default.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        public Query(string table) : this(table, new ConnectionManager())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="creator">Connection manager to be used.</param>
        public Query(string table, ConnectionCreator creator) : this(new DbName(table), creator)
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="manager">Connection manager to be used.</param>
        public Query(string table, ConnectionManager manager) : this(new DbName(table), manager)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/> using the default values ​​defined in ConnectionCreator.Default.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        public Query(DbName table) : this(table, new ConnectionManager())
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="creator">Connection manager to be used.</param>
        public Query(DbName table, ConnectionCreator creator) : this(table, new ConnectionManager(creator))
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="manager">Connection manager to be used.</param>
        public Query(DbName table, ConnectionManager manager) : base(manager.Config, table)
        {
            this.Manager = manager;
        }

        private Query(DbName table, QueryConfig config) : base(config, table)
        {

        }

        #endregion

        #region Selection

        /// <summary>
        /// Select keys of table by table.
        /// </summary>
        /// <param table="columnNames"></param>
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
        /// <param table="columns"></param>
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

        /// <summary>
        /// Perform a INNER JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="column1">The name of the column from the current table to be used in the tables.</param>
        /// <param name="column2">The name of the column from the specified table to be used in the tables.</param>
        /// <returns></returns>
        public Query Join(string table, string column1, string column2)
        {
            return this.Join(table, q => q.WhereColumn(column1, column2));
        }

        /// <summary>
        /// Perform a JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="column1">The name of the column from the current table to be used in the tables.</param>
        /// <param name="operation">SQL operation to be used for comparing the columns.</param>
        /// <param name="column2">The name of the column from the specified table to be used in the tables.</param>
        /// <param name="type">Type of tables between the tables.</param>
        /// <returns></returns>
        public Query Join(string table, string column1, string operation, string column2, string type = "INNER")
        {
            return this.Join(table, q => q.WhereColumn(column1, operation, column2), type);
        }

        /// <summary>
        /// Perform a JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="callback">Callback used to build the comparison for the tables.</param>
        /// <param name="grammarOptions">Options of the grammar for the tables.</param>
        /// <param name="type">Type of tables between the tables.</param>
        /// <returns></returns>
        public Query Join(string table, QueryCallback callback, string type = "INNER", object grammarOptions = null)
        {
            return this.Join(new DbName(table), callback, type, grammarOptions);
        }

        /// <summary>
        /// Perform a JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="callback">Callback used to build the comparison for the tables.</param>
        /// <param name="type">Type of tables between the tables.</param>
        /// <returns></returns>
        public Query Join(DbName table, QueryCallback callback, string type = "INNER")
        {
            return this.Join(table, callback, type, this.GrammarOptions);
        }

        /// <summary>
        /// Perform a JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="callback">Callback used to build the comparison for the tables.</param>
        /// <param name="grammarOptions">Options of the grammar for the tables.</param>
        /// <param name="type">Type of tables between the tables.</param>
        /// <returns></returns>
        public Query Join(DbName table, QueryCallback callback, string type = "INNER", object grammarOptions = null)
        {
            JoinQuery join = new JoinQuery(this.Info.Config, table) { Type = type, GrammarOptions = grammarOptions };
            callback(join);
            this.Info.Joins.Add(join);
            return this;
        }

        #endregion

        #region GroupBy

        /// <summary>
        /// Group the results of the query by the specified criteria (Add a GROUP BY clause to the query.).
        /// </summary>
        /// <param name="columnNames">The column names by which the results should be grouped.</param>
        /// <returns></returns>
        public Query GroupBy(params string[] columnNames)
        {
            return this.GroupBy(columnNames.Select(name => new Column(name)).ToArray());
        }

        /// <summary>
        /// Group the results of the query by the specified criteria.
        /// </summary>
        /// <param name="columns">The columns by which the results should be grouped.</param>
        /// <returns></returns>
        public Query GroupBy(params Column[] columns)
        {
            this.Info.GroupsBy = columns;

            return this;
        }

        #endregion

        #region Having

        /// <summary>
        /// Add a HAVING clause to the query based on a callback.
        /// </summary>
        /// <param name="callback">The callback that defines the conditions of the HAVING clause.</param>
        /// <returns></returns>
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
        /// <param table="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(params string[] columns)
        {
            return this.OrderBy(SharpOrm.OrderBy.Asc, columns);
        }

        /// <summary>
        /// Applies descending sort.
        /// </summary>
        /// <param table="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderByDesc(params string[] columns)
        {
            return this.OrderBy(SharpOrm.OrderBy.Desc, columns);
        }

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param table="order">Field ordering.</param>
        /// <param table="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(OrderBy order, params string[] columns)
        {
            return this.OrderBy(order, columns.Select(c => new Column(c)).ToArray());
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param table="order">Field ordering.</param>
        /// <param table="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(OrderBy order, params Column[] columns)
        {
            return this.OrderBy(columns.Select(c => new ColumnOrder(c, order)).ToArray());
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param table="orders"></param>
        /// <returns></returns>
        public Query OrderBy(params ColumnOrder[] orders)
        {
            this.Info.Orders = orders.Where(x => x.Order != SharpOrm.OrderBy.None).ToArray();

            return this;
        }

        #endregion

        #region DML SQL commands

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param table="cells"></param>
        /// <returns></returns>
        public int Update(params Cell[] cells)
        {
            this.ValidateReadonly();
            if (!cells.Any())
                throw new InvalidOperationException(Messages.NoColumnsInserted);

            return this.Update((IEnumerable<Cell>)cells);
        }

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param table="cells"></param>
        /// <returns></returns>
        public int Update(IEnumerable<Cell> cells)
        {
            this.ValidateReadonly();
            this.CheckIsSafeOperation();
            return this.ExecuteAndGetAffected(this.GetGrammar().Update(cells));
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param table="cells"></param>
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
        /// <param table="cells"></param>
        /// <returns>Id of row.</returns>
        public int Insert(IEnumerable<Cell> cells)
        {
            this.ValidateReadonly();
            object result = this.ExecuteScalar(this.GetGrammar().Insert(cells));
            return TranslationUtils.IsNumeric(result?.GetType()) ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Insert a lot of values ​​using the result of a table (select command);
        /// </summary>
        /// <param table="query"></param>
        /// <param table="columnNames"></param>
        public int Insert(QueryBase query, params string[] columnNames)
        {
            this.ValidateReadonly();
            object result = this.ExecuteScalar(this.GetGrammar().InsertQuery(query, columnNames));
            return TranslationUtils.IsNumeric(result?.GetType()) ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Insert a lot of values ​​using the result of a table (select command);
        /// </summary>
        /// <param table="columnNames"></param>
        public int Insert(SqlExpression expression, params string[] columnNames)
        {
            this.ValidateReadonly();
            return this.ExecuteAndGetAffected(this.GetGrammar().InsertExpression(expression, columnNames));
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param table="rows"></param>
        public int BulkInsert(params Row[] rows)
        {
            return this.BulkInsert((ICollection<Row>)rows);
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param table="rows"></param>
        public int BulkInsert(IEnumerable<Row> rows)
        {
            this.ValidateReadonly();
            return this.ExecuteAndGetAffected(this.GetGrammar().BulkInsert(rows));
        }

        /// <summary>
        /// Removes rows from database
        /// </summary>
        /// <returns></returns>
        public int Delete()
        {
            this.ValidateReadonly();
            this.CheckIsSafeOperation();
            return this.ExecuteAndGetAffected(this.GetGrammar().Delete());
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            this.ValidateReadonly();
            return Convert.ToInt64(this.ExecuteScalar(this.GetGrammar().Count()));
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <param table="column">Column to count.</param>
        /// <returns></returns>
        public long Count(string columnName)
        {
            return this.Count(new Column(columnName));
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <param table="column">Column to count.</param>
        /// <returns></returns>
        public long Count(Column column)
        {
            this.ValidateReadonly();
            return Convert.ToInt64(this.ExecuteScalar(this.GetGrammar().Count(column)));
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

        /// <summary>
        /// Retrieves an enumerable collection of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the enumerable collection.</typeparam>
        /// <returns>An enumerable collection of the specified type.</returns>
        public virtual IEnumerable<T> GetEnumerable<T>()
        {
            this.ValidateReadonly();
            this.Token.ThrowIfCancellationRequested();
            var grammar = this.Info.Config.NewGrammar(this);

            return new DbCommandEnumerable<T>(this.GetCommand(grammar.Select()), this.Config.Translation, this.Manager.Management, this.Token) { DisposeCommand = true };
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other keys are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public T[] ExecuteArrayScalar<T>()
        {
            this.ValidateReadonly();
            this.Token.ThrowIfCancellationRequested();
            try
            {
                using (DbCommand cmd = this.GetCommand(this.GetGrammar().Select()))
                    return cmd.ExecuteArrayScalar<T>(this.Config.Translation).ToArray();
            }
            finally
            {
                this.Manager?.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public T ExecuteScalar<T>()
        {
            this.ValidateReadonly();
            return this.Config.Translation.FromSql<T>(this.ExecuteScalar(this.GetGrammar().Select()));
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public object ExecuteScalar()
        {
            this.ValidateReadonly();
            return this.Config.Translation.FromSql(this.ExecuteScalar(this.GetGrammar().Select()));
        }

        /// <summary>
        /// Execute SQL Select command and return Collections.
        /// </summary>
        /// <returns></returns>
        public DbDataReader ExecuteReader()
        {
            this.ValidateReadonly();
            this.Token.ThrowIfCancellationRequested();

            if (this.lastOpenReader is OpenReader last)
                last.Dispose();

            return (this.lastOpenReader = new OpenReader(this.GetCommand(this.GetGrammar().Select()))).reader;
        }

        /// <summary>
        /// Indicate that the rows from the joined tables that matched the criteria should also be removed when executing <see cref="Delete"/>.
        /// </summary>
        /// <param name="tables">Names of the tables (or aliases if used) whose rows should be removed.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Query JoinToDelete(params string[] tables)
        {
            if (tables == null || tables.Length == 0)
                throw new ArgumentNullException(nameof(tables));

            this.deleteJoins = tables.Select(this.Config.ApplyNomenclature).ToArray();
            return this;
        }

        private void ValidateReadonly()
        {
            if (this.Manager is null)
                throw new InvalidOperationException("This query is read-only.");
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
        /// <param table="withWhere">Signals if the parameters of the "WHERE" should be copied.</param>
        /// <returns></returns>
        public virtual Query Clone(bool withWhere)
        {
            Query query = new Query(this.Info.TableName, this.Manager);

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

        #region SQL Execution

        protected internal object ExecuteScalar(SqlExpression expression)
        {
            return this.SafeExecuteCommand(expression, cmd => cmd.ExecuteScalar());
        }

        protected internal int ExecuteNonQuery(SqlExpression expression)
        {
            return this.SafeExecuteCommand(expression, cmd => cmd.ExecuteNonQuery());
        }

        protected internal int ExecuteAndGetAffected(SqlExpression expression)
        {
            return this.SafeExecuteCommand(expression, cmd =>
            {
                using (var reader = cmd.ExecuteReader())
                    return reader.RecordsAffected;
            });
        }

        protected T SafeExecuteCommand<T>(SqlExpression expression, Func<DbCommand, T> func)
        {
            try
            {
                using (var cmd = this.GetCommand(expression))
                    return func(cmd);
            }
            finally
            {
                this.Manager.CloseByEndOperation();
            }
        }

        protected internal DbCommand GetCommand(SqlExpression expression)
        {
            Grammar.QueryLogger?.Invoke(expression.ToString());
            return this.Manager
                .CreateCommand(this.CommandTimeout)
                .SetExpression(expression)
                .SetCancellationToken(this.Token);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.lastOpenReader is OpenReader last)
                last.Dispose();

            this.Manager?.CloseByDisposeChild();
            this.lastOpenReader = null;
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return GetGrammar().SelectSqlOnly();
        }

        /// <summary>
        /// Create a <see cref="SqlExpression"/> that represents a select in SQL.
        /// </summary>
        /// <returns></returns>
        public SqlExpression ToSqlExpression()
        {
            return GetGrammar().GetSelectExpression();
        }

        protected internal Grammar GetGrammar()
        {
            return this.Info.Config.NewGrammar(this);
        }

        private class OpenReader : IDisposable
        {
            private readonly DbCommand command;
            public readonly DbDataReader reader;
            private bool _disposed;

            public OpenReader(DbCommand command)
            {
                this.command = command;
                this.reader = command.ExecuteReader();
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
                try { this.command.Dispose(); } catch { }
            }
        }
    }
}
