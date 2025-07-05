using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.Builder.Grammars;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using SharpOrm.ForeignKey;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm
{
    /// <summary>
    /// Class responsible for interacting with the data of a database table.
    /// </summary>
    /// <typeparam name="T">Type that should be used to interact with the table.</typeparam>
    public class Query<T> : Query, IFkNodeRoot, INodeCreationListener
    {
        private ObjectReader _objReader;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _pendingSelect = false;

        protected internal TableInfo TableInfo { get; }
        private readonly ForeignKeyRegister _foreignKeyRegister;

        /// <summary>
        /// If the model has one or more validations defined, they will be checked before saving or updating.
        /// </summary>
        public bool ValidateModelOnSave { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore timestamps during operations.
        /// </summary>
        public bool IgnoreTimestamps { get; set; }

        /// <summary>
        /// Gets or sets the visibility of items marked as deleted.
        /// </summary>
        public Trashed Trashed
        {
            get => Info.Where.Trashed;
            set => Info.Where.SetTrash(value, TableInfo);
        }

        ForeignKeyRegister IFkNodeRoot.ForeignKeyRegister => _foreignKeyRegister;

        #region Query

        /// <summary>
        /// Creates a read-only query for the specified table.
        /// </summary>
        /// <param name="alias">The alias of the table.</param>
        /// <param name="config">The configuration for the query. If null, the default configuration is used.</param>
        /// <returns>A read-only query for the specified table.</returns>
        public static new Query<T> ReadOnly(string alias, QueryConfig config = null)
        {
            return new Query<T>(new DbName(alias), config ?? ConnectionCreator.Default?.Config);
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/> using the default values ​​defined in ConnectionCreator.Default.
        /// </summary>
        public Query() : this(new DbName(GetDbName()), ConnectionCreator.Default)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="creator">Connection manager to be used.</param>
        public Query(ConnectionCreator creator) : this(new DbName(GetDbName(creator.Config?.Translation), null), creator)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="manager">Connection manager to be used.</param>
        public Query(ConnectionManager manager) : this(new DbName(GetDbName(manager.Config?.Translation), null), manager)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/> using the default values ​​defined in ConnectionCreator.Default.
        /// </summary>
        /// <param name="alias">Alias for the table.</param>
        public Query(string alias) : this(new DbName(GetDbName(), alias))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="alias">Alias for the table.</param>
        /// <param name="creator">Connection manager to be used.</param>
        public Query(string alias, ConnectionCreator creator) : this(new DbName(GetDbName(), alias), creator)
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="Query"/>.
        /// </summary>
        /// <param name="alias">Alias for the table.</param>
        /// <param name="manager">Connection manager to be used.</param>
        public Query(string alias, ConnectionManager manager) : this(new DbName(GetDbName(manager.Config?.Translation), alias), manager)
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
            Info.Parent = this;
            TableInfo = manager.Config.Translation.GetTable(typeof(T));
            ValidateModelOnSave = manager.Config.ValidateModelOnSave;
            ApplyValidations();

            if (TableInfo.SoftDelete != null)
                Trashed = Trashed.Except;

            _foreignKeyRegister = new ForeignKeyRegister(TableInfo, Info.TableName, this);
        }

        private Query(DbName table, QueryConfig config) : base(table, config)
        {
            Info.Parent = this;
            _foreignKeyRegister = new ForeignKeyRegister(TableInfo, Info.TableName, this);
        }

        private void ApplyValidations()
        {
            ReturnsInsetionId = TableInfo.GetPrimaryKeys().Length > 0;
        }

        private static string GetDbName(TranslationRegistry registry = null)
        {
            if (registry == null) registry = TranslationRegistry.Default;

            return registry.GetTableName(typeof(T));
        }
        #endregion

        #region OrderBy

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query<T> OrderBy(Expression<ColumnExpression<T>> expression)
        {
            return OrderBy(SharpOrm.OrderBy.Asc, expression);
        }

        /// <summary>
        /// Applies descending sort.
        /// </summary>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query<T> OrderByDesc(Expression<ColumnExpression<T>> expression)
        {
            return OrderBy(SharpOrm.OrderBy.Desc, expression);
        }

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="order">Field ordering.</param>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query<T> OrderBy(OrderBy order, Expression<ColumnExpression<T>> expression)
        {
            return (Query<T>)OrderBy(order, GetColumns(expression));
        }

        /// <summary>
        /// Group the results of the query by the specified criteria (Add a GROUP BY clause to the query.).
        /// </summary>
        /// <param name="columnNames">The column names by which the results should be grouped.</param>
        /// <returns></returns>
        public Query<T> GroupBy(Expression<ColumnExpression<T>> expression)
        {
            return (Query<T>)base.GroupBy(GetColumns(expression));
        }

        /// <summary>
        /// Selects a column from the table using a column expression.
        /// </summary>
        /// <typeparam name="K">The type of the column value.</typeparam>
        /// <param name="column">The column expression to select.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> SelectColumn<K>(Expression<ColumnExpression<T, K>> column)
        {
            return Select(GetColumn(column));
        }

        /// <summary>
        /// Select column of table by Column object.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Query<T> Select(Expression<ColumnExpression<T>> expression)
        {
            return (Query<T>)base.Select(GetColumns(expression));
        }

        /// <summary>
        /// Select keys of table by table.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public new Query<T> Select(params string[] columnNames)
        {
            return (Query<T>)base.Select(columnNames);
        }

        /// <summary>
        /// Select column of table by Column object.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public new Query<T> Select(params Column[] columns)
        {
            return (Query<T>)base.Select(columns);
        }

        #endregion

        internal ExpressionColumn[] GetColumns(Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            var processor = new ExpressionProcessor<T>(this, config);
            return processor.ParseColumns(expression).ToArray();
        }

        #region AddForeign

        public IIncludable<T, TProperty> Include<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var members = ExpressionUtils<T>.GetMemberPath(expression, false).Reverse();
            return _foreignKeyRegister.RegisterTreePath(members).GetIncludable<T, TProperty>();
        }

        /// <summary>
        /// Adds a foreign key to the query based on the specified column expression.
        /// </summary>
        /// <param name="call">An expression representing the column to be added as a foreign key.</param>
        /// <returns>The query with the added foreign key.</returns>
        public Query<T> AddForeign(Expression<ColumnExpression<T>> call)
        {
            var members = ExpressionUtils<T>.GetMemberPath(call, false).Reverse();
            _foreignKeyRegister.RegisterTreePath(members);

            return this;
        }

        void INodeCreationListener.Created(ForeignKeyNode node)
        {
            if (node.IsCollection || node.ParentIsCollection)
                return;

            Info.Joins.Add(node.ToJoinQuery(Info.Config));
            _pendingSelect = true;
        }

        #endregion

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

        #region DML SQL commands

        /// <summary>
        /// Asynchronously deletes rows from the database.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the numb
        public override async Task<int> DeleteAsync(CancellationToken token)
        {
            return await DeleteAsync(false, token);
        }

        /// <inheritdoc/>
        public override int Delete()
        {
            return Delete(false);
        }

        /// <summary>
        /// Asynchronously deletes rows from the database, with an option to force deletion.
        /// </summary>
        /// <param name="force">If true, forces deletion even if soft delete is enabled; otherwise, performs a soft delete if applicable.</param>
        /// <returns>A task representing the asynchronous operation, with the number of deleted rows.</returns>
        public async Task<int> DeleteAsync(bool force, CancellationToken token)
        {
            if (force || TableInfo.SoftDelete == null)
                return await base.DeleteAsync(token);

            using (var cmd = GetCommand().AddCancellationToken(token))
                return await cmd.SetExpressionWithAffectedRowsAsync(GetGrammar().SoftDelete(TableInfo.SoftDelete)) + await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Remove the database rows (if it's a class with soft delete, just mark it as deleted).
        /// </summary>
        /// <param name="force">If the class uses soft delete and it's set to false, mark the row as deleted; otherwise, delete the row.</param>
        /// <returns>Number of deleted rows.</returns>
        public int Delete(bool force)
        {
            if (force || TableInfo.SoftDelete == null)
                return base.Delete();

            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().SoftDelete(TableInfo.SoftDelete)) + cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Asynchronously restores soft-deleted records in the database.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the number of restored rows.</returns>
        public async Task<int> RestoreAsync(CancellationToken token)
        {
            using (var cmd = GetCommand().AddCancellationToken(token))
                return await cmd.SetExpressionWithAffectedRowsAsync(GetGrammar().RestoreSoftDeleted(TableInfo.SoftDelete)) +
                     await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Restore the values deleted using soft delete.
        /// </summary>
        /// <returns>Number of values restored.</returns>
        /// <exception cref="NotSupportedException">Launched when there is an attempt to restore a class that does not implement soft delete.</exception>
        public int Restore()
        {
            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().RestoreSoftDeleted(TableInfo.SoftDelete)) +
                    cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result asynchronously.
        /// </summary>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number (One based).</param>
        /// <param name="countColumn">Column used to count the number of items.</param>
        /// <returns>A task representing the asynchronous operation, with a Pager<T> object for performing pagination on the query result.</returns>
        public Task<Pager<T>> PaginateAsync(int peerPage, int currentPage, Column countColumn)
        {
            return Pager<T>.FromBuilderAsync(this, peerPage, currentPage, countColumn);
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result.
        /// </summary>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number (One based).</param>
        /// <param name="countColumn">Column used to count the number of items.</param>
        /// <returns>A Pager<T> object for performing pagination on the query result.</returns>
        public Pager<T> Paginate(int peerPage, int currentPage, Column countColumn)
        {
            return Pager<T>.FromBuilder(this, peerPage, currentPage, countColumn);
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result asynchronously.
        /// </summary>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number (One based).</param>
        /// <param name="countColumnName">Column name used to count the number of items.</param>
        /// <returns>A task representing the asynchronous operation, with a Pager<T> object for performing pagination on the query result.</returns>
        public Task<Pager<T>> PaginateAsync(int peerPage, int currentPage, string countColumnName)
        {
            return Pager<T>.FromBuilderAsync(this, peerPage, currentPage, countColumnName);
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result.
        /// </summary>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number (One based).</param>
        /// <param name="countColumnName">Column name used to count the number of items.</param>
        /// <returns>A Pager<T> object for performing pagination on the query result.</returns>
        public Pager<T> Paginate(int peerPage, int currentPage, string countColumnName)
        {
            return Pager<T>.FromBuilder(this, peerPage, currentPage, countColumnName);
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result asynchronously.
        /// </summary>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number (One based).</param>
        /// <returns>A task representing the asynchronous operation, with a Pager<T> object for performing pagination on the query result.</returns>
        public Task<Pager<T>> PaginateAsync(int peerPage, int currentPage)
        {
            return Pager<T>.FromBuilderAsync(this, peerPage, currentPage);
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

        /// <summary>
        /// Retrieves an enumerable collection of the specified type.
        /// </summary>
        /// <returns>An enumerable collection of the specified type.</returns>
        public IEnumerable<T> GetEnumerable()
        {
            return base.GetEnumerable<T>();
        }

        /// <summary>
        /// Asynchronously retrieves the first result or the default value if no result is found.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the first result or the default value.</returns>
        public Task<T> FirstOrDefaultAsync(CancellationToken token)
        {
            return TaskUtils.Async(() =>
            {
                int? lastLimit = Limit;
                Limit = 1;

                try
                {
                    return GetEnumerable<T>(token).FirstOrDefault();
                }
                finally
                {
                    Limit = lastLimit;
                }
            });
        }

        /// <summary>
        /// Get first result.
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault()
        {
            int? lastLimit = Limit;
            Limit = 1;

            try
            {
                return GetEnumerable<T>().FirstOrDefault();
            }
            finally
            {
                Limit = lastLimit;
            }
        }

        public override IEnumerable<K> GetEnumerable<K>()
        {
            return GetEnumerable<K>(default);
        }

        private IEnumerable<K> GetEnumerable<K>(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                ConfigurePendingColumns();
                using (var cmd = GetCommand().AddCancellationToken(token).SetExpression(Info.Config.NewGrammar(this).Select()))
                    foreach (var item in ConfigureFkLoader(cmd.ExecuteEnumerable<K>(true)))
                        yield return item;
            }
            finally
            {
                Manager?.CloseByEndOperation();
            }
        }

        private IEnumerable<K> ConfigureFkLoader<K>(DbCommandEnumerable<K> enumerable)
        {
            FkLoaders fkLoaders = new FkLoaders(Manager, _foreignKeyRegister, Token);

            enumerable._fkQueue = fkLoaders;
            var list = enumerable.ToList();
            fkLoaders.LoadForeigns();
            return list;
        }

        /// <summary>
        /// Asynchronously searches and returns the first occurrence of an object of type T that matches the values of the provided primary keys.
        /// </summary>
        /// <param name="primaryKeysValues">The values of the primary keys to search for.</param>
        /// <returns>A task representing the asynchronous operation, with the first occurrence of an object of type T that matches the provided primary keys.</returns>
        public Task<T> FindAsync(CancellationToken token, params object[] primaryKeysValues)
        {
            return TaskUtils.Async(() => Find(primaryKeysValues));
        }

        /// <summary>
        /// Searches and returns the first occurrence of an object of type T that matches the values of the provided primary keys.
        /// </summary>
        /// <param name="primaryKeysValues">The values of the primary keys to search for.</param>
        /// <returns>The first occurrence of an object of type T that matches the provided primary keys.</returns>
        public T Find(params object[] primaryKeysValues)
        {
            using (var query = (Query<T>)Clone(false))
                return WherePk(primaryKeysValues).FirstOrDefault();
        }

        /// <summary>
        /// AddRaws a clause to retrieve the items that have the primary key of the object.
        /// </summary>
        /// <param name="primaryKeysValues">Primary keys.</param>
        /// <returns></returns>
        public Query<T> WherePk(params object[] primaryKeysValues)
        {
            if (primaryKeysValues.Length == 0) throw new ArgumentNullException(nameof(primaryKeysValues));

            var pkCols = TableInfo.GetPrimaryKeys();
            ValidatePkVals(pkCols, primaryKeysValues);

            if (primaryKeysValues.Length == 1)
                return (Query<T>)Where(pkCols[0].Name, primaryKeysValues[0]);

            return (Query<T>)Where(query =>
            {
                for (var i = 0; i < primaryKeysValues.Length; i++)
                    query.Where(pkCols[i].Name, primaryKeysValues[i]);
            });
        }

        /// <summary>
        /// Asynchronously gets all available results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with an array of results.</returns>
        public Task<T[]> GetAsync(CancellationToken token)
        {
            return TaskUtils.Async(() =>
            {
                using (var builder = GetCommand().AddCancellationToken(token))
                    return GetEnumerable<T>(builder, token).ToArray();
            });
        }

        /// <summary>
        /// Get all available results.
        /// </summary>
        /// <returns></returns>
        public T[] Get()
        {
            return GetEnumerable<T>().ToArray();
        }

        /// <summary>
        /// Asynchronously inserts one row into the table.
        /// </summary>
        /// <param name="obj">The object to insert.</param>
        /// <returns>A task representing the asynchronous operation, with the ID of the inserted row.</returns>
        public async Task<int> InsertAsync(T obj, CancellationToken token)
        {
            ValidateReadonly();

            var reader = GetObjectReader(ReadMode.ValidOnly, true);
            object result = await InsertAsync(reader.ReadCells(obj), ReturnsInsetionId && !reader.HasValidKey(obj), token);
            SetPrimaryKey(obj, result);
            return TranslationUtils.TryNumeric(result);
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Id of row.</returns>
        public int Insert(T obj)
        {
            ValidateReadonly();

            var reader = GetObjectReader(ReadMode.ValidOnly, true);
            object result = Insert(reader.ReadCells(obj), ReturnsInsetionId && !reader.HasValidKey(obj));
            SetPrimaryKey(obj, result);
            return TranslationUtils.TryNumeric(result);
        }

        private void SetPrimaryKey(T owner, object result)
        {
            if (!Config.ApplyGeneratedKey) return;

            var keys = TableInfo.GetPrimaryKeys();
            if (keys.Length != 1) return;
            var key = keys.First();

            if (object.Equals(ReflectionUtils.GetDefault(key.Type), key.Get(owner)))
                key.Set(owner, result);
        }

        /// <summary>
        /// Asynchronously inserts one or more rows into the table.
        /// </summary>
        /// <param name="objs">The objects to insert.</param>
        /// <returns>A task representing the asynchronous operation, with the number of inserted rows.</returns>
        public Task<int> BulkInsertAsync(CancellationToken token, params T[] objs)
        {
            return BulkInsertAsync((IEnumerable<T>)objs, token);
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(params T[] objs)
        {
            return BulkInsert((IEnumerable<T>)objs);
        }

        /// <summary>
        /// Asynchronously inserts one or more rows into the table.
        /// </summary>
        /// <param name="objs">The objects to insert.</param>
        /// <returns>A task representing the asynchronous operation, with the number of inserted rows.</returns>
        public Task<int> BulkInsertAsync(IEnumerable<T> objs, CancellationToken token)
        {
            var reader = GetObjectReader(ReadMode.ValidOnly, true);
            return base.BulkInsertAsync(objs.Select(x => reader.ReadRow(x)), token);
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        ///<param name="objs"></param>
        public int BulkInsert(IEnumerable<T> objs)
        {
            var reader = GetObjectReader(ReadMode.ValidOnly, true);
            return base.BulkInsert(objs.Select(x => reader.ReadRow(x)));
        }

        /// <summary>
        /// Asynchronously updates table keys using object values.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        /// <returns>A task representing the asynchronous operation, with the number of updated rows.</returns>
        public Task<int> UpdateAsync(T obj, CancellationToken token)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return base.UpdateAsync(GetObjectReader(ReadMode.None, false).ReadCells(obj), token);
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

            return base.Update(GetObjectReader(ReadMode.None, false).ReadCells(obj));
        }

        /// <summary>
        /// Asynchronously updates table keys using object values.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        /// <param name="expression">Expression to retrieve the properties that should be saved.</param>
        /// <returns>A task representing the asynchronous operation, with the number of updated rows.</returns>
        public Task<int> UpdateAsync(T obj, Expression<ColumnExpression<T>> expression, CancellationToken token)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return UpdateAsync(GetObjectReader(ReadMode.None, false).Only(expression).ReadCells(obj), token);
        }

        /// <summary>
        /// Update table keys using object values.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="expression">Expression to retrieve the properties that should be saved.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public int Update(T obj, Expression<ColumnExpression<T>> expression)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return Update(GetObjectReader(ReadMode.None, false).Only(expression).ReadCells(obj));
        }

        /// <summary>
        /// Asynchronously updates table keys using object values.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        /// <param name="columns">The columns to update.</param>
        /// <returns>A task representing the asynchronous operation, with the number of updated rows.</returns>
        public Task<int> UpdateAsync(T obj, CancellationToken token, params string[] columns)
        {
            return base.UpdateAsync(GetUpdateCells(obj, columns), token);
        }

        /// <summary>
        /// Update table keys using object values.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="columns">Update table keys using object values..</param>
        /// <returns></returns>
        public int Update(T obj, params string[] columns)
        {
            return base.Update(GetUpdateCells(obj, columns));
        }

        private Cell[] GetUpdateCells(T obj, params string[] columns)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (columns.Length == 0)
                throw new ArgumentNullException(nameof(columns));

            var reader = GetObjectReader(ReadMode.None, false);
            if (columns.Length > 0) reader.Only(columns);

            var toUpdate = reader.ReadCells(obj).ToArray();
            if (toUpdate.Length == 0)
                throw new InvalidOperationException(Messages.ColumnsNotFound);

            return toUpdate;
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists. If null, all columns will be updated.</param>
        public Task UpsertAsync(T obj, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp = null, CancellationToken token = default)
        {
            var processor = new ExpressionProcessor<T>(this, ExpressionConfig.New);
            var toCheckColumns = processor.ParseColumnNames(toCheckColumnsExp).ToArray();
            var updateColumns = processor.ParseColumnNames(updateColumnsExp).ToArray();

            return UpsertAsync(obj, toCheckColumns, updateColumns, default);
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="objs">Objects to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists. If null, all columns will be updated.</param>
        public Task<int> UpsertAsync(T[] objs, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp = null, CancellationToken token = default)
        {
            var processor = new ExpressionProcessor<T>(this, ExpressionConfig.New);
            var toCheckColumns = processor.ParseColumnNames(toCheckColumnsExp).ToArray();
            var updateColumns = processor.ParseColumnNames(updateColumnsExp).ToArray();

            return UpsertAsync(objs, toCheckColumns, updateColumns, default);
        }

        /// <summary>
        /// Inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists. If null, all columns will be updated.</param>
        public void Upsert(T obj, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp = null)
        {
            var processor = new ExpressionProcessor<T>(this, ExpressionConfig.New);
            var toCheckColumns = processor.ParseColumnNames(toCheckColumnsExp).ToArray();
            var updateColumns = processor.ParseColumnNames(updateColumnsExp).ToArray();

            Upsert(obj, toCheckColumns, updateColumns);
        }

        /// <summary>
        /// Inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="objs">The object to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists. If null, all columns will be updated.</param>
        public int Upsert(T[] objs, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp = null)
        {
            var processor = new ExpressionProcessor<T>(this, ExpressionConfig.New);
            var toCheckColumns = processor.ParseColumnNames(toCheckColumnsExp).ToArray();
            var updateColumns = processor.ParseColumnNames(updateColumnsExp).ToArray();

            return Upsert(objs, toCheckColumns, updateColumns);
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        public Task UpsertAsync(T obj, string[] toCheckColumns, string[] updateColumns, CancellationToken token)
        {
            if (toCheckColumns.Length < 1)
                throw new ArgumentException(Messages.AtLeastOneColumnRequired, nameof(toCheckColumns));

            return base.UpsertAsync(GetObjectReader(ReadMode.ValidOnly, true).ReadRow(obj), toCheckColumns, updateColumns, token);
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="objs">Objects to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        public async Task<int> UpsertAsync(T[] objs, string[] toCheckColumns, string[] updateColumns, CancellationToken token)
        {
            if (objs.Length == 0)
                return 0;

            if (toCheckColumns.Length < 1)
                throw new ArgumentException(Messages.AtLeastOneColumnRequired, nameof(toCheckColumns));

            return await base.UpsertAsync(GetObjectReader(ReadMode.ValidOnly, true).ReadRows(objs), toCheckColumns, updateColumns, token);
        }

        /// <summary>
        /// Inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <exception cref="ArgumentException">Thrown when no columns are specified to check.</exception>
        public void Upsert(T obj, string[] toCheckColumns, params string[] updateColumns)
        {
            if (toCheckColumns.Length < 1)
                throw new ArgumentException(Messages.AtLeastOneColumnRequired, nameof(toCheckColumns));

            base.Upsert(GetObjectReader(ReadMode.ValidOnly, true).ReadRow(obj), toCheckColumns, updateColumns);
        }

        /// <summary>
        /// Inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="objs">Objects to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <exception cref="ArgumentException">Thrown when no columns are specified to check.</exception>
        public int Upsert(T[] objs, string[] toCheckColumns, params string[] updateColumns)
        {
            if (objs.Length == 0)
                return 0;

            if (toCheckColumns.Length < 1)
                throw new ArgumentException(Messages.AtLeastOneColumnRequired, nameof(toCheckColumns));

            return base.Upsert(GetObjectReader(ReadMode.ValidOnly, true).ReadRows(objs), toCheckColumns, updateColumns);
        }

        /// <summary>
        /// Inserts or updates an object in the database based on the specified columns to check, update, and insert.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists.</param>
        /// <param name="insertColumnsExp">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumnsExp"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public void Upsert(T obj, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp, Expression<ColumnExpression<T>> insertColumnsExp, bool excludeInserColumns = false)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var processor = new ExpressionProcessor<T>(this, ExpressionConfig.New);
            var toCheckColumns = processor.ParseColumnNames(toCheckColumnsExp).ToArray();
            var updateColumns = processor.ParseColumnNames(updateColumnsExp).ToArray();
            var insertColumns = processor.ParseColumnNames(insertColumnsExp).ToArray();

            base.Upsert(GetObjectReader(ReadMode.ValidOnly, true).ReadRow(obj), toCheckColumns, updateColumns, insertColumns, excludeInserColumns);
        }

        /// <summary>
        /// Inserts or updates an object in the database based on the specified columns to check, update, and insert.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumns"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
        public void Upsert(T obj, string[] toCheckColumns, string[] updateColumns, string[] insertColumns, bool excludeInserColumns = false)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            base.Upsert(GetObjectReader(ReadMode.ValidOnly, true).ReadRow(obj), toCheckColumns, updateColumns, insertColumns, excludeInserColumns);
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check, update, and insert.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists.</param>
        /// <param name="insertColumnsExp">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumnsExp"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpsertAsync(T obj, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp, Expression<ColumnExpression<T>> insertColumnsExp, bool excludeInserColumns = false, CancellationToken token = default)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var processor = new ExpressionProcessor<T>(this, ExpressionConfig.New);
            var toCheckColumns = processor.ParseColumnNames(toCheckColumnsExp).ToArray();
            var updateColumns = processor.ParseColumnNames(updateColumnsExp).ToArray();
            var insertColumns = processor.ParseColumnNames(insertColumnsExp).ToArray();

            await base.UpsertAsync(GetObjectReader(ReadMode.ValidOnly, true).ReadRow(obj), toCheckColumns, updateColumns, insertColumns, excludeInserColumns, token);
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check, update, and insert.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumns"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpsertAsync(T obj, string[] toCheckColumns, string[] updateColumns, string[] insertColumns, bool excludeInserColumns = false, CancellationToken token = default)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            await base.UpsertAsync(GetObjectReader(ReadMode.ValidOnly, true).ReadRow(obj), toCheckColumns, updateColumns, insertColumns, excludeInserColumns, token);
        }

        #endregion

        #region Join

        /// <summary>
        /// Performs a LEFT JOIN between this query and another table.
        /// </summary>
        /// <typeparam name="R">The type of the related table.</typeparam>
        /// <param name="table">The table expression to join.</param>
        /// <param name="column1">The first column expression to compare.</param>
        /// <param name="column2">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> LeftJoin<R>(Expression<ColumnExpression<T, R>> table, Expression<ColumnExpression<R>> column1, Expression<ColumnExpression<T>> column2)
        {
            return Join(table, column1, "=", column2, "LEFT");
        }

        /// <summary>
        /// Performs a LEFT JOIN between this query and another table with a specified operation.
        /// </summary>
        /// <typeparam name="R">The type of the related table.</typeparam>
        /// <param name="table">The table expression to join.</param>
        /// <param name="column1">The first column expression to compare.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="column2">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> LeftJoin<R>(Expression<ColumnExpression<T, R>> table, Expression<ColumnExpression<R>> column1, string operation, Expression<ColumnExpression<T>> column2)
        {
            return Join(table, column1, operation, column2, "LEFT");
        }

        /// <summary>
        /// Performs an INNER JOIN between this query and another table.
        /// </summary>
        /// <typeparam name="R">The type of the related table.</typeparam>
        /// <param name="table">The table expression to join.</param>
        /// <param name="column1">The first column expression to compare.</param>
        /// <param name="column2">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> Join<R>(Expression<ColumnExpression<T, R>> table, Expression<ColumnExpression<R>> column1, Expression<ColumnExpression<T>> column2)
        {
            return Join(table, column1, "=", column2);
        }

        /// <summary>
        /// Performs a JOIN between this query and another table with a specified operation and join type.
        /// </summary>
        /// <typeparam name="R">The type of the related table.</typeparam>
        /// <param name="table">The table expression to join.</param>
        /// <param name="column1">The first column expression to compare.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="column2">The second column expression to compare.</param>
        /// <param name="alias">The alias for the table.</param>
        /// <param name="type">The type of join (e.g., "INNER", "LEFT").</param>
        /// <returns>The current query instance.</returns>
        public Query<T> Join<R>(Expression<ColumnExpression<T, R>> table, Expression<ColumnExpression<R>> column1, string operation, Expression<ColumnExpression<T>> column2, string alias = null, string type = "INNER", object grammarOptions = null)
        {
            var members = ExpressionUtils<T>.GetMemberPath(table, false).Reverse().ToArray();
            var node = _foreignKeyRegister.Get(members);

            if (node != null)
                throw new InvalidOperationException(string.Format(Messages.Query.DuplicateJoin, members.Last().Name));

            node = _foreignKeyRegister.RegisterTreePath(members, true);

            JoinQuery join = new JoinQuery(Info.Config, node.Name) { Type = type, GrammarOptions = grammarOptions };
            (join.Info as QueryInfo).Parent = this;

            var column1Result = GetColumn(column1, node);
            var column2Result = GetColumn(column2);

            join.Where(column1Result, operation, column2Result);
            Info.Joins.Add(join);

            _pendingSelect = true;

            return this;
        }

        public Query<T> Join<R>(string alias, string column1, string column2)
        {
            var dbName = DbName.Of<R>(alias, Config.Translation);
            base.Join(dbName, x => x.WhereColumn(column1, column2), "INNER");
            return this;
        }

        /// <summary>
        /// Perform a INNER JOIN between this query and another table.
        /// </summary>
        public new Query<T> Join(string table, string column1, string column2)
        {
            return (Query<T>)base.Join(table, column1, column2);
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
        public new Query<T> Join(string table, string column1, string operation, string column2, string type = "INNER")
        {
            return (Query<T>)base.Join(table, column1, operation, column2, type);
        }

        /// <summary>
        /// Perform a JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="callback">Callback used to build the comparison for the tables.</param>
        /// <param name="grammarOptions">Options of the grammar for the tables.</param>
        /// <param name="type">Type of tables between the tables.</param>
        /// <returns></returns>
        public new Query<T> Join(string table, QueryCallback callback, string type = "INNER", object grammarOptions = null)
        {
            return (Query<T>)base.Join(table, callback, type, grammarOptions);
        }

        /// <summary>
        /// Perform a JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="callback">Callback used to build the comparison for the tables.</param>
        /// <param name="type">Type of tables between the tables.</param>
        /// <returns></returns>
        public new Query<T> Join(DbName table, QueryCallback callback, string type = "INNER")
        {
            return (Query<T>)base.Join(table, callback, type);
        }

        /// <summary>
        /// Perform a JOIN between this query and another table.
        /// </summary>
        /// <param name="table">The name of the table to which you want to tables.</param>
        /// <param name="callback">Callback used to build the comparison for the tables.</param>
        /// <param name="grammarOptions">Options of the grammar for the tables.</param>
        /// <param name="type">Type of tables between the tables.</param>
        /// <returns></returns>
        public new Query<T> Join(DbName table, QueryCallback callback, string type = "INNER", object grammarOptions = null)
        {
            return (Query<T>)base.Join(table, callback, type, grammarOptions);
        }

        #endregion

        #region GroupBy

        /// <summary>
        /// Group the results of the query by the specified criteria (Add a GROUP BY clause to the query.).
        /// </summary>
        /// <param name="columnNames">The column names by which the results should be grouped.</param>
        /// <returns></returns>
        public new Query<T> GroupBy(params string[] columnNames)
        {
            return (Query<T>)base.GroupBy(columnNames);
        }

        /// <summary>
        /// Group the results of the query by the specified criteria.
        /// </summary>
        /// <param name="columns">The columns by which the results should be grouped.</param>
        /// <returns></returns>
        public new Query<T> GroupBy(params Column[] columns)
        {
            return (Query<T>)base.GroupBy(columns);
        }

        #endregion

        #region Having

        /// <summary>
        /// Add a HAVING clause to the query based on a callback.
        /// </summary>
        /// <param name="callback">The callback that defines the conditions of the HAVING clause.</param>
        /// <returns></returns>
        public new Query<T> Having(QueryCallback callback)
        {
            return (Query<T>)base.Having(callback);
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public new Query<T> OrderBy(params string[] columns)
        {
            return (Query<T>)base.OrderBy(columns);
        }

        /// <summary>
        /// Applies descending sort.
        /// </summary>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public new Query<T> OrderByDesc(params string[] columns)
        {
            return (Query<T>)base.OrderByDesc(columns);
        }

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="order">Field ordering.</param>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public new Query<T> OrderBy(OrderBy order, params string[] columns)
        {
            return (Query<T>)base.OrderBy(order, columns);
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param name="order">Field ordering.</param>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public new Query<T> OrderBy(OrderBy order, params Column[] columns)
        {
            return (Query<T>)base.OrderBy(order, columns);
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public new Query<T> OrderBy(params ColumnOrder[] orders)
        {
            return (Query<T>)base.OrderBy(orders);
        }

        #endregion

        #region Where

        /// <summary>
        /// Adds a WHERE clause that checks if the column contains the value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to filter.</param>
        /// <param name="column">The column on which the "contains" condition is applied.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public Query<T> WhereContains(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.WhereContains(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public Query<T> WhereStartsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.WhereStartsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public Query<T> WhereEndsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.WhereEndsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not contains the value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to filter.</param>
        /// <param name="column">The column on which the "contains" condition is applied.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public Query<T> WhereNotContains(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.WhereNotContains(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public Query<T> WhereNotStartsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.WhereNotStartsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public Query<T> WhereNotEndsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.WhereNotEndsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column does not equal the specified value.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> WhereNot(Expression<ColumnExpression<T>> columnExp, object value)
        {
            base.WhereNot(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column equals the specified value.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> Where(Expression<ColumnExpression<T>> columnExp, object value)
        {
            base.Where(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause with a specified operation and value.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> Where(Expression<ColumnExpression<T>> columnExp, string operation, object value)
        {
            Where(GetColumn(columnExp), operation, value);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that compares two columns for equality.
        /// </summary>
        /// <param name="columnExp">The first column expression to compare.</param>
        /// <param name="column2Exp">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> WhereColumn(Expression<ColumnExpression<T>> columnExp, Expression<ColumnExpression<T>> column2Exp)
        {
            base.Where(GetColumn(columnExp), GetColumn(column2Exp));
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that compares two columns with a specified operation.
        /// </summary>
        /// <param name="columnExp">The first column expression to compare.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="column2Exp">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> WhereColumn(Expression<ColumnExpression<T>> columnExp, string operation, Expression<ColumnExpression<T>> column2Exp)
        {
            Where(GetColumn(columnExp), operation, GetColumn(column2Exp));
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the first column does not equal the second column.
        /// </summary>
        /// <param name="columnExp">The first column expression to compare.</param>
        /// <param name="column2Exp">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> WhereNotColumn(Expression<ColumnExpression<T>> columnExp, Expression<ColumnExpression<T>> column2Exp)
        {
            base.Where(GetColumn(columnExp), "!=", GetColumn(column2Exp));
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column value is between two specified values.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="arg1">The first value to compare.</param>
        /// <param name="arg2">The second value to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> WhereBetween(Expression<ColumnExpression<T>> columnExp, object arg1, object arg2)
        {
            return (Query<T>)base.WhereBetween(GetColumn(columnExp), arg1, arg2);
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column value is not between two specified values.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="arg1">The first value to compare.</param>
        /// <param name="arg2">The second value to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> WhereNotBetween(Expression<ColumnExpression<T>> columnExp, object arg1, object arg2)
        {
            return (Query<T>)base.WhereNotBetween(GetColumn(columnExp), arg1, arg2);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public Query<T> WhereIn<K>(Expression<ColumnExpression<T>> columnExp, ICollection<K> items)
        {
            return (Query<T>)base.Where(GetColumn(columnExp), "IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public Query<T> WhereIn<K>(Expression<ColumnExpression<T>> columnExp, params K[] items)
        {
            return (Query<T>)base.Where(GetColumn(columnExp), "IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the column value is not among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public Query<T> WhereNotIn<K>(Expression<ColumnExpression<T>> columnExp, params K[] items)
        {
            return (Query<T>)base.Where(GetColumn(columnExp), "NOT IN", items);
        }

        public Query<T> WhereNotIn<K>(Expression<ColumnExpression<T>> columnExp, ICollection<K> items)
        {
            return (Query<T>)base.Where(GetColumn(columnExp), "NOT IN", items);
        }

        /// <summary>
        /// This method adds a clause to the "WHERE" clause checking if a column is null
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        public Query<T> WhereNull(Expression<ColumnExpression<T>> columnExp)
        {
            return (Query<T>)base.Where(GetColumn(columnExp), "IS", null);
        }

        /// <summary>
        /// This method adds a clause to the "WHERE" clause checking if a column is not null
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        public Query<T> WhereNotNull(Expression<ColumnExpression<T>> columnExp)
        {
            return (Query<T>)Where(GetColumn(columnExp), "IS NOT", null);
        }

        #region OR

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column contains the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "contains" comparison on.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public Query<T> OrWhereContains(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.OrWhereContains(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public Query<T> OrWhereStartsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.OrWhereStartsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public Query<T> OrWhereEndsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.OrWhereEndsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not contains the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "contains" comparison on.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public Query<T> OrWhereNotContains(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.OrWhereNotContains(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public Query<T> OrWhereNotStartsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.OrWhereNotStartsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public Query<T> OrWhereNotEndsWith(Expression<ColumnExpression<T>> columnExp, string value)
        {
            this.OrWhereNotEndsWith(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column does not equal the specified value.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereNot(Expression<ColumnExpression<T>> columnExp, object value)
        {
            base.OrWhereNot(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column equals the specified value.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhere(Expression<ColumnExpression<T>> columnExp, object value)
        {
            base.OrWhere(GetColumn(columnExp), value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause with a specified operation and value.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhere(Expression<ColumnExpression<T>> columnExp, string operation, object value)
        {
            OrWhere(GetColumn(columnExp), operation, value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that compares two columns for equality.
        /// </summary>
        /// <param name="columnExp">The first column expression to compare.</param>
        /// <param name="column2Exp">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereColumn(Expression<ColumnExpression<T>> columnExp, Expression<ColumnExpression<T>> column2Exp)
        {
            base.OrWhere(GetColumn(columnExp), GetColumn(column2Exp));
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that compares two columns with a specified operation.
        /// </summary>
        /// <param name="columnExp">The first column expression to compare.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="column2Exp">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereColumn(Expression<ColumnExpression<T>> columnExp, string operation, Expression<ColumnExpression<T>> column2Exp)
        {
            OrWhere(GetColumn(columnExp), operation, GetColumn(column2Exp));
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the first column does not equal the second column.
        /// </summary>
        /// <param name="columnExp">The first column expression to compare.</param>
        /// <param name="column2Exp">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereNotColumn(Expression<ColumnExpression<T>> columnExp, Expression<ColumnExpression<T>> column2Exp)
        {
            base.OrWhere(GetColumn(columnExp), "!=", GetColumn(column2Exp));
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column value is between two specified values.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="arg1">The first value to compare.</param>
        /// <param name="arg2">The second value to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereBetween(Expression<ColumnExpression<T>> columnExp, object arg1, object arg2)
        {
            return (Query<T>)base.OrWhereBetween(GetColumn(columnExp), arg1, arg2);
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column value is not between two specified values.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="arg1">The first value to compare.</param>
        /// <param name="arg2">The second value to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereNotBetween(Expression<ColumnExpression<T>> columnExp, object arg1, object arg2)
        {
            return (Query<T>)base.OrWhereNotBetween(GetColumn(columnExp), arg1, arg2);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="K">The type of items to compare.</typeparam>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereIn<K>(Expression<ColumnExpression<T>> columnExp, params K[] items)
        {
            return (Query<T>)base.OrWhere(GetColumn(columnExp), "IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the column value is not among the specified items.
        /// </summary>
        /// <typeparam name="K">The type of items to compare.</typeparam>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereNotIn<K>(Expression<ColumnExpression<T>> columnExp, params K[] items)
        {
            return (Query<T>)base.OrWhere(GetColumn(columnExp), "NOT IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column is null.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereNull(Expression<ColumnExpression<T>> columnExp)
        {
            return (Query<T>)base.OrWhere(GetColumn(columnExp), "IS", null);
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column is not null.
        /// </summary>
        /// <param name="columnExp">The column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> OrWhereNotNull(Expression<ColumnExpression<T>> columnExp)
        {
            return (Query<T>)OrWhere(GetColumn(columnExp), "IS NOT", null);
        }

        #endregion

        #endregion

        private ExpressionColumn GetColumn<K>(Expression<ColumnExpression<K>> column, IForeignKeyNode parent = null)
        {
            var processor = new ExpressionProcessor<K>(Info, Config.Translation, ExpressionConfig.SubMembers | ExpressionConfig.Method, parent ?? _foreignKeyRegister);

            return processor.ParseColumns(column).First();
        }

        private Column GetColumn<K>(Expression<ColumnExpression<T, K>> column)
        {
            var processor = new ExpressionProcessor<T>(this, ExpressionConfig.All);
            return processor.ParseColumn(column);
        }

        /// <summary>
        /// Clones the Query object.
        /// </summary>
        /// <param name="withWhere">Indicates if the parameters of the "WHERE" clause should be copied.</param>
        /// <returns>A new instance of the Query object with the same configuration.</returns>
        public override Query Clone(bool withWhere)
        {
            Query<T> query = new Query<T>(Info.TableName, Manager);
            query.Token = Token;
            query._pendingSelect = _pendingSelect;

            if (withWhere)
                query.Info.LoadFrom(Info);
            else if (TableInfo.SoftDelete != null)
                query.Info.Where.SetTrash(Trashed, TableInfo);

            OnClone(query);

            return query;
        }

        protected override void OnClone(Query cloned)
        {
            base.OnClone(cloned);

            if (!(cloned is Query<T> query))
                return;

            _foreignKeyRegister.CopyTo(query._foreignKeyRegister);
        }

        internal ObjectReader GetObjectReader(ReadMode pkReadMode, bool isCreate)
        {
            if (_objReader == null)
            {
                _objReader = new ObjectReader(TableInfo);
                _objReader.ReadFk = Info.Config.LoadForeign;
                _objReader.Validate = ValidateModelOnSave;
            }

            _objReader.IgnoreTimestamps = IgnoreTimestamps;
            _objReader.PrimaryKeyMode = pkReadMode;
            _objReader.IsCreate = isCreate;

            return _objReader;
        }

        public override string ToString()
        {
            ConfigurePendingColumns();
            return base.ToString();
        }

        private void ConfigurePendingColumns()
        {
            if (!_pendingSelect)
                return;

            _pendingSelect = false;
            _foreignKeyRegister.ApplySelectToQuery(this);
        }

        public override QueryBase Where(object column, string operation, object value)
        {
            if (column is string strCol)
                return base.Where(new SafeWhere(strCol, operation, value));

            if (column is Column col)
                return base.Where(new SafeWhere(col, operation, value));

            return base.Where(column, operation, value);
        }

        public override QueryBase OrWhere(object column, string operation, object value)
        {
            if (column is string strCol)
                return base.OrWhere(new SafeWhere(strCol, operation, value));

            if (column is Column col)
                return base.OrWhere(new SafeWhere(col, operation, value));

            return base.OrWhere(column, operation, value);
        }
    }

    /// <summary>
    /// Class responsible for interacting with the data of a database table.
    /// </summary>
    public class Query : QueryBase, ICloneable, IGrammarOptions, IDisposable
    {
        #region Properties
        private bool _disposed = false;

        protected internal new QueryInfo Info => (QueryInfo)base.Info;

        /// <summary>
        /// Gets a value indicating whether the object has been _disposed.
        /// </summary>
        public bool Disposed => _disposed;

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
        public QueryConfig Config => Info.Config;
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
            get => _commandTimeout ?? Manager.CommandTimeout;
            set => _commandTimeout = value;
        }

        private CommandBuilder _lastOpenReader = null;
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
        public Query(DbName table, ConnectionManager manager) : this(table, manager.Config)
        {
            Manager = manager;
        }

        internal Query(DbName table, QueryConfig config) : base(new QueryInfo(config, table))
        {

        }

        #endregion

        #region Selection

        /// <summary>
        /// Select keys of table by table.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public Query Select(params string[] columnNames)
        {
            if (!columnNames.Any())
                throw new ArgumentNullException(nameof(columnNames), Messages.NoColumnsInserted);

            if (columnNames.Length == 1 && columnNames.First() == "*")
                return Select(Column.All);

            return Select(columnNames.Select(name => new Column(name)).ToArray());
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

            Info.Select = columns;

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
            return Join(table, q => q.WhereColumn(column1, column2));
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
            return Join(table, q => q.WhereColumn(column1, operation, column2), type);
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
            return Join(new DbName(table), callback, type, grammarOptions);
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
            return Join(table, callback, type, GrammarOptions);
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
            JoinQuery join = new JoinQuery(Info.Config, table) { Type = type, GrammarOptions = grammarOptions };
            callback(join);
            Info.Joins.Add(join);
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
            return GroupBy(columnNames.Select(name => new Column(name)).ToArray());
        }

        /// <summary>
        /// Group the results of the query by the specified criteria.
        /// </summary>
        /// <param name="columns">The columns by which the results should be grouped.</param>
        /// <returns></returns>
        public Query GroupBy(params Column[] columns)
        {
            Info.GroupsBy = columns;

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
            var qBase = new QueryBase(Config, Info.TableName);
            callback(qBase);
            Info.Having.Add(qBase.Info.Where);

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
            return OrderBy(SharpOrm.OrderBy.Asc, columns);
        }

        /// <summary>
        /// Applies descending sort.
        /// </summary>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderByDesc(params string[] columns)
        {
            return OrderBy(SharpOrm.OrderBy.Desc, columns);
        }

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="order">Field ordering.</param>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(OrderBy order, params string[] columns)
        {
            return OrderBy(order, columns.Select(c => new Column(c)).ToArray());
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param name="order">Field ordering.</param>
        /// <param name="columns">Columns that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(OrderBy order, params Column[] columns)
        {
            return OrderBy(columns.Select(c => new ColumnOrder(c, order)).ToArray());
        }

        /// <summary>
        /// Applies sorting to the query.
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public Query OrderBy(params ColumnOrder[] orders)
        {
            Info.Orders = orders.Where(x => x.Order != SharpOrm.OrderBy.None).ToArray();

            return this;
        }

        #endregion

        #region DML SQL Commands

        /// <summary>
        /// Asynchronously inserts or updates a record in the database based on the specified columns to check, update, and insert.
        /// </summary>
        /// <param name="sourceName">The name of the source table.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <returns>A task representing the asynchronous operation, with the number of affected rows.</returns>
        public async Task<int> UpsertAsync(DbName sourceName, string[] toCheckColumns, string[] updateColumns, string[] insertColumns, CancellationToken token)
        {
            ValidateUpsert(sourceName, toCheckColumns, updateColumns, insertColumns);

            using (var cmd = GetCommand().AddCancellationToken(token))
                return await cmd.SetExpressionWithAffectedRowsAsync(GetGrammar().Upsert(sourceName, toCheckColumns, updateColumns, insertColumns)) +
                    await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Inserts or updates a record in the database based on the specified columns to check, update, and insert.
        /// </summary>
        /// <param name="sourceName">The name of the source table.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any of the column arrays are null or empty.</exception>
        public int Upsert(DbName sourceName, string[] toCheckColumns, string[] updateColumns, string[] insertColumns)
        {
            ValidateUpsert(sourceName, toCheckColumns, updateColumns, insertColumns);

            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().Upsert(sourceName, toCheckColumns, updateColumns, insertColumns)) +
                    cmd.ExecuteNonQuery();
        }

        private static void ValidateUpsert(DbName sourceName, string[] toCheckColumns, string[] updateColumns, string[] insertColumns)
        {
            if (toCheckColumns == null || toCheckColumns.Length == 0)
                throw new ArgumentNullException(nameof(toCheckColumns), "ToCheckColumns cannot be null or empty.");

            if (updateColumns == null || updateColumns.Length == 0)
                throw new ArgumentNullException(nameof(updateColumns), "UpdateColumns cannot be null or empty.");

            if (insertColumns == null || insertColumns.Length == 0)
                throw new ArgumentNullException(nameof(insertColumns), "InsertColumns cannot be null or empty.");
        }

        /// <summary>
        /// Asynchronously inserts or updates a row in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="row">The row to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpsertAsync(Row row, string[] toCheckColumns, string[] updateColumns = null, CancellationToken token = default)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            ValidateReadonly();

            await new UpsertStrategy(this, row)
                .SetCheckColumns(toCheckColumns)
                .SetUpdateColumns(updateColumns)
                .UpsertAsync(token);
        }

        /// <summary>
        /// Inserts or updates a row in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="row">The row to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <exception cref="ArgumentNullException">Thrown when the row or toCheckColumns are null or empty.</exception>
        public void Upsert(Row row, string[] toCheckColumns, string[] updateColumns = null)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            new UpsertStrategy(this, row)
               .SetCheckColumns(toCheckColumns)
               .SetUpdateColumns(updateColumns)
               .Upsert();
        }

        /// <summary>
        /// Asynchronously inserts or updates multiple rows in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="rows">The rows to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <returns>A task representing the asynchronous operation, with the number of affected rows.</returns>
        public async Task<int> UpsertAsync(Row[] rows, string[] toCheckColumns, string[] updateColumns = null, CancellationToken token = default)
        {
            return await new UpsertStrategy(this, rows)
                .SetCheckColumns(toCheckColumns)
                .SetUpdateColumns(updateColumns)
                .UpsertAsync(token);
        }

        /// <summary>
        /// Asynchronously inserts or updates multiple rows in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="rows">The rows to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumns"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        /// <returns>A task representing the asynchronous operation, with the number of affected rows.</returns>
        public async Task<int> UpsertAsync(Row[] rows, string[] toCheckColumns, string[] updateColumns, string[] insertColumns, bool excludeInserColumns = false, CancellationToken token = default)
        {
            return await new UpsertStrategy(this, rows)
                .SetCheckColumns(toCheckColumns)
                .SetUpdateColumns(updateColumns)
                .SetInsertColumns(excludeInserColumns, insertColumns)
                .UpsertAsync(token);
        }

        /// <summary>
        /// Inserts or updates multiple rows in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="rows">The rows to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentNullException">Thrown when rows or toCheckColumns are null or empty.</exception>
        public int Upsert(Row[] rows, string[] toCheckColumns, string[] updateColumns = null)
        {
            if (rows.Length == 0)
                return 0;

            return new UpsertStrategy(this, rows)
               .SetCheckColumns(toCheckColumns)
               .SetUpdateColumns(updateColumns)
               .Upsert();
        }

        /// <summary>
        /// Inserts or updates multiple rows in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="rows">The rows to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumns"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentNullException">Thrown when rows or toCheckColumns are null or empty.</exception>
        public int Upsert(Row[] rows, string[] toCheckColumns, string[] updateColumns, string[] insertColumns, bool excludeInserColumns = false)
        {
            if (rows.Length == 0)
                return 0;

            return new UpsertStrategy(this, rows)
               .SetCheckColumns(toCheckColumns)
               .SetUpdateColumns(updateColumns)
               .SetInsertColumns(excludeInserColumns, insertColumns)
               .Upsert();
        }

        /// <summary>
        /// Inserts or updates a row in the database based on the specified columns to check, update, and insert.
        /// </summary>
        /// <param name="row">The row to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumns"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        public void Upsert(Row row, string[] toCheckColumns, string[] updateColumns, string[] insertColumns, bool excludeInserColumns = false)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            new UpsertStrategy(this, row)
               .SetCheckColumns(toCheckColumns)
               .SetUpdateColumns(updateColumns)
               .SetInsertColumns(excludeInserColumns, insertColumns)
               .Upsert();
        }

        /// <summary>
        /// Asynchronously inserts or updates a row in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="row">The row to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist. If null, all columns will be inserted.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <param name="insertColumns">The columns to insert if a record does not exist.</param>
        /// <param name="excludeInserColumns">If true, the columns specified in <paramref name="insertColumns"/> will be excluded from the insert operation; otherwise, only those columns will be included.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpsertAsync(Row row, string[] toCheckColumns, string[] updateColumns, string[] insertColumns, bool excludeInserColumns = false, CancellationToken token = default)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            await new UpsertStrategy(this, row)
                .SetCheckColumns(toCheckColumns)
                .SetUpdateColumns(updateColumns)
                .SetInsertColumns(excludeInserColumns, insertColumns)
                .UpsertAsync(token);
        }

        /// <summary>
        /// Asynchronously updates rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(CancellationToken token, params Cell[] cells)
        {
            ValidateReadonly();
            if (!cells.Any())
                throw new InvalidOperationException(Messages.NoColumnsInserted);

            return await UpdateAsync((IEnumerable<Cell>)cells, token);
        }

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public int Update(params Cell[] cells)
        {
            ValidateReadonly();
            if (!cells.Any())
                throw new InvalidOperationException(Messages.NoColumnsInserted);

            return Update((IEnumerable<Cell>)cells);
        }

        /// <summary>
        /// Asynchronously updates rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(IEnumerable<Cell> cells, CancellationToken token)
        {
            ValidateReadonly();
            CheckIsSafeOperation();

            using (var cmd = GetCommand().AddCancellationToken(token))
                return await cmd.SetExpressionWithAffectedRowsAsync(GetGrammar().Update(cells)) +
                    await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public int Update(IEnumerable<Cell> cells)
        {
            CheckIsSafeOperation();

            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().Update(cells)) + cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Asynchronously inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Id of row.</returns>
        public async Task<int> InsertAsync(CancellationToken token, params Cell[] cells)
        {
            ValidateReadonly();

            if (cells.Length == 0)
                throw new InvalidOperationException(Messages.AtLeastOneColumnRequired);

            return TranslationUtils.TryNumeric(await InsertAsync(cells, true, token));
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

            return TranslationUtils.TryNumeric(Insert(cells, true));
        }

        /// <summary>
        /// Asynchronously inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Id of row.</returns>
        public async Task<int> InsertAsync(IEnumerable<Cell> cells, CancellationToken token)
        {
            ValidateReadonly();

            return TranslationUtils.TryNumeric(await InsertAsync(cells, true, token));
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Id of row.</returns>
        public int Insert(IEnumerable<Cell> cells)
        {
            return TranslationUtils.TryNumeric(Insert(cells, true));
        }

        /// <summary>
        /// Asynchronously inserts a new record into the database table using the specified query and column names.
        /// </summary>
        /// <param name="queryBase">The base query to use for the insert operation.</param>
        /// <param name="columnNames">The names of the columns to insert values into.</param>
        /// <returns>A task representing the asynchronous operation, with the number of affected rows.</returns>
        public async Task<int> InsertAsync(QueryBase queryBase, CancellationToken token, params string[] columnNames)
        {
            using (var cmd = await GetCommand().AddCancellationToken(token).SetExpressionAsync(GetGrammar().InsertQuery(queryBase, columnNames)))
                return await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Insert a lot of values ​​using the result of a table (select command);
        /// </summary>
        /// <param name="columnNames"></param>
        public int Insert(QueryBase queryBase, params string[] columnNames)
        {
            using (var cmd = GetCommand().SetExpression(GetGrammar().InsertQuery(queryBase, columnNames)))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Insert a lot of values ​​using the result of a table (select command);
        /// </summary>
        /// <param name="columnNames"></param>
        public int Insert(SqlExpression expression, params string[] columnNames)
        {
            if (columnNames == null || columnNames.Length == 0)
                throw new InvalidOperationException(Messages.AtLeastOneColumnRequired);

            using (var cmd = GetCommand().SetExpression(GetGrammar().InsertExpression(expression, columnNames)))
                return cmd.ExecuteNonQuery();
        }

        internal async Task<object> InsertAsync(IEnumerable<Cell> cells, bool returnsInsetionId, CancellationToken token)
        {
            ValidateReadonly();

            using (var cmd = GetCommand().AddCancellationToken(token))
            {
                await cmd.SetExpressionAsync(GetGrammar().Insert(cells, returnsInsetionId));
                return await cmd.ExecuteScalarAsync();
            }
        }

        internal object Insert(IEnumerable<Cell> cells, bool returnsInsetionId)
        {
            using (var cmd = GetCommand())
            {
                cmd.SetExpression(GetGrammar().Insert(cells, returnsInsetionId));
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Asynchronously inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public async Task<int> BulkInsertAsync(CancellationToken token, params Row[] rows)
        {
            return await BulkInsertAsync((ICollection<Row>)rows, token);
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(params Row[] rows)
        {
            return BulkInsert((ICollection<Row>)rows);
        }

        /// <summary>
        /// Asynchronously inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public async Task<int> BulkInsertAsync(IEnumerable<Row> rows, CancellationToken token)
        {
            using (var cmd = GetCommand().AddCancellationToken(token))
            {
                var expression = GetGrammar().BulkInsert(rows);
                if (expression.IsEmpty)
                    return 0;

                return await cmd.SetExpressionWithAffectedRowsAsync(expression) +
                    await cmd.ExecuteWithRecordsAffectedAsync();
            }
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(IEnumerable<Row> rows)
        {
            using (var cmd = GetCommand())
            {
                var expression = GetGrammar().BulkInsert(rows);
                if (expression.IsEmpty)
                    return 0;

                return cmd.SetExpressionWithAffectedRows(expression) + cmd.ExecuteWithRecordsAffected();
            }
        }

        /// <summary>
        /// Asynchronously removes rows from database
        /// </summary>
        /// <returns>Number of deleted rows.</returns>
        public virtual async Task<int> DeleteAsync(CancellationToken token)
        {
            ValidateReadonly();
            CheckIsSafeOperation();

            using (var cmd = GetCommand().AddCancellationToken(token))
                return await cmd.SetExpressionWithAffectedRowsAsync(GetGrammar().Delete()) +
                    await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Asynchronously deletes rows from the database, including those from joined tables.
        /// </summary>
        /// <param name="tables">The names of the tables to include in the delete operation. These should be the same names used in .Join.</param>
        /// <returns>A task representing the asynchronous operation, with the number of deleted rows.</returns>
        /// <remarks>
        /// This function is not compatible with all databases.
        /// </remarks>
        public async Task<int> DeleteIncludingJoinsAsync(CancellationToken token, params string[] tables)
        {
            if (tables == null || tables.Length == 0)
                throw new ArgumentNullException(nameof(tables));

            CheckIsSafeOperation();

            using (var cmd = GetCommand().AddCancellationToken(token))
                return await cmd.SetExpressionWithAffectedRowsAsync(GetGrammar().DeleteIncludingJoins(tables)) + await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Deletes rows from the database, including those from joined tables.
        /// </summary>
        /// <param name="tables">The names of the tables to include in the delete operation. These should be the same names used in .Join.</param>
        /// <returns>The number of deleted rows.</returns>
        /// <remarks>
        /// This function is not compatible with all databases.
        /// </remarks>
        public int DeleteIncludingJoins(params string[] tables)
        {
            if (tables == null || tables.Length == 0)
                throw new ArgumentNullException(nameof(tables));

            CheckIsSafeOperation();

            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().DeleteIncludingJoins(tables)) + cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Removes rows from database
        /// </summary>
        /// <returns>Number of deleted rows.</returns>
        public virtual int Delete()
        {
            CheckIsSafeOperation();

            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().Delete()) + cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Asynchronously counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public async Task<long> CountAsync(CancellationToken token)
        {
            ValidateReadonly();
            using (var cmd = GetCommand().AddCancellationToken(token).SetExpression(GetGrammar().Count()))
                return Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            using (var cmd = GetCommand().SetExpression(GetGrammar().Count()))
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Asynchronously counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public async Task<long> CountAsync(string columnName, CancellationToken token)
        {
            return await CountAsync(new Column(columnName), token);
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public long Count(string columnName)
        {
            return Count(new Column(columnName));
        }

        /// <summary>
        /// Asynchronously counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public async Task<long> CountAsync(Column column, CancellationToken token)
        {
            using (var cmd = GetCommand().AddCancellationToken(token).SetExpression(GetGrammar().Count(column)))
                return Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public long Count(Column column)
        {
            using (var cmd = GetCommand().SetExpression(GetGrammar().Count(column)))
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Asynchronously returns all rows of the table
        /// </summary>
        /// <returns></returns>
        public Task<Row[]> ReadRowsAsync(CancellationToken token)
        {
            return TaskUtils.Async(() =>
            {
                using (var cmd = GetCommand())
                    return GetEnumerable<Row>(cmd, token).ToArray();
            });
        }

        /// <summary>
        /// Returns all rows of the table
        /// </summary>
        /// <returns></returns>
        public Row[] ReadRows()
        {
            return GetEnumerable<Row>().ToArray();
        }

        /// <summary>
        /// Asynchronously returns the first row of the table (if the table returns no value, null will be returned).
        /// </summary>
        /// <returns></returns>
        public Task<Row> FirstRowAsync(CancellationToken token)
        {
            return TaskUtils.Async(() =>
            {
                int? lastLimit = Limit;
                Limit = 1;

                try
                {
                    using (var cmd = GetCommand())
                        return GetEnumerable<Row>(cmd, token).FirstOrDefault();
                }
                finally
                {
                    Limit = lastLimit;
                }
            });
        }

        /// <summary>
        /// Returns the first row of the table (if the table returns no value, null will be returned).
        /// </summary>
        /// <returns></returns>
        public Row FirstRow()
        {
            int? lastLimit = Limit;
            Limit = 1;

            try
            {
                using (var cmd = GetCommand())
                    return GetEnumerable<Row>(cmd, default).FirstOrDefault();
            }
            finally
            {
                Limit = lastLimit;
            }
        }

        /// <summary>
        /// Asynchronously retrieves an collection of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <returns>An collection of the specified type.</returns>
        public Task<T[]> GetAsync<T>(CancellationToken token)
        {
            return TaskUtils.Async(() =>
            {
                using (var cmd = GetCommand())
                    return GetEnumerable<T>(cmd, token).ToArray();
            });
        }

        /// <summary>
        /// Retrieves an collection of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <returns>An collection of the specified type.</returns>
        public T[] Get<T>()
        {
            using (var cmd = GetCommand())
                return GetEnumerable<T>(cmd, default).ToArray();
        }

        /// <summary>
        /// Retrieves an enumerable collection of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the enumerable collection.</typeparam>
        /// <returns>An enumerable collection of the specified type.</returns>
        public virtual IEnumerable<T> GetEnumerable<T>()
        {
            Token.ThrowIfCancellationRequested();

            using (var cmd = GetCommand(true))
                foreach (var item in GetEnumerable<T>(cmd, default))
                    yield return item;
        }

        internal IEnumerable<T> GetEnumerable<T>(CommandBuilder builder, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return builder.SetExpression(Info.Config.NewGrammar(this).Select())
                .SetExpression(Info.Config.NewGrammar(this).Select())
                .AddCancellationToken(token)
                .ExecuteEnumerable<T>(builder._leaveOpen);
        }

        /// <summary>
        /// Asynchronously executes the query and returns the first column of all rows in the result. All other keys are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public async Task<T[]> ExecuteArrayScalarAsync<T>(CancellationToken token)
        {
            Token.ThrowIfCancellationRequested();

            using (var cmd = GetCommand().AddCancellationToken(token).SetExpression(GetGrammar().Select()))
                return await cmd.ExecuteArrayScalarAsync<T>();
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other keys are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public T[] ExecuteArrayScalar<T>()
        {
            Token.ThrowIfCancellationRequested();

            using (var cmd = GetCommand().SetExpression(GetGrammar().Select()))
                return cmd.ExecuteArrayScalar<T>();

        }

        /// <summary>
        /// Asynchronously executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public async Task<T> ExecuteScalarAsync<T>(CancellationToken token)
        {
            using (var cmd = GetCommand().AddCancellationToken(token).SetExpression(GetGrammar().Select()))
                return await cmd.ExecuteScalarAsync<T>();
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public T ExecuteScalar<T>()
        {
            using (var cmd = GetCommand().SetExpression(GetGrammar().Select()))
                return cmd.ExecuteScalar<T>();
        }

        /// <summary>
        /// Asynchronously executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public async Task<object> ExecuteScalarAsync(CancellationToken token)
        {
            using (var cmd = GetCommand().AddCancellationToken(token).SetExpression(GetGrammar().Select()))
                return await cmd.ExecuteScalarAsync();
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other keys and rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public object ExecuteScalar()
        {
            using (var cmd = GetCommand().SetExpression(GetGrammar().Select()))
                return cmd.ExecuteScalar();
        }

        /// <summary>
        /// Asynchronously executes the query and returns a <see cref="DbDataReader"/> to read the results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with a <see cref="DbDataReader"/> to read the results.</returns>
        public Task<DbDataReader> ExecuteReaderAsync()
        {
            return TaskUtils.Async(ExecuteReader);
        }

        /// <summary>
        /// Execute SQL Select command and return Collections.
        /// </summary>
        /// <returns></returns>
        public DbDataReader ExecuteReader()
        {
            Token.ThrowIfCancellationRequested();

            if (_lastOpenReader is CommandBuilder last)
                last.Dispose();

            return (_lastOpenReader = GetCommand().SetExpression(GetGrammar().Select())).ExecuteReader();
        }

        #endregion

        #region Clone and safety

        /// <summary>
        /// Clones the Query object with the parameters of "WHERE".
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return Clone(true);
        }

        /// <summary>
        /// Clones the Query object.
        /// </summary>
        /// <param name="withWhereJoin">Signals if the parameters of the "WHERE" should be copied.</param>
        /// <returns></returns>
        public virtual Query Clone(bool withWhereJoin)
        {
            Query query = new Query(Info.TableName, Manager);
            query.Token = Token;

            if (withWhereJoin)
                query.Info.LoadFrom(Info);

            OnClone(query);

            return query;
        }

        /// <summary>
        /// Throws an error if only operations with "WHERE" are allowed and there are none configured.
        /// </summary>
        protected void CheckIsSafeOperation()
        {
            if (Info.Config.OnlySafeModifications && Info.Where.Empty)
                throw new UnsafeDbOperation();
        }

        protected virtual void OnClone(Query cloned)
        {
            cloned.Distinct = Distinct;
            cloned.Limit = Limit;
            cloned.Offset = Offset;
        }

        #endregion

        /// <summary>
        /// Adds an EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="table">The table name to be checked for the existence of a record.</param>
        ///<param name="callback">Callback to build condition</param>
        /// <returns>A Query instance for method chaining.</returns>
        public Query Exists(string table, QueryCallback callback)
        {
            var query = Query.ReadOnly(table, Info.Config);
            query.Limit = 1;
            query.Where(callback);
            base.Exists(query);
            return this;
        }

        /// <summary>
        /// Adds an EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="query">The table name to be checked for the existence of a record.</param>
        ///<param name="callback">Callback to build condition</param>
        /// <returns>A Query instance for method chaining.</returns>
        public Query OrExists(string table, QueryCallback callback)
        {
            var query = Query.ReadOnly(table, Info.Config);
            query.Limit = 1;
            query.Where(callback);
            base.OrExists(query);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause to the query based on another QueryBase instance.
        /// </summary>
        /// <param name="where">The QueryBase instance to add to the WHERE clause.</param>
        /// <returns>The current query instance.</returns>
        public Query Where(QueryBase where)
        {
            if (where is Query) throw new NotSupportedException(string.Format(Messages.Query.InvalidWhereValue, where.GetType().FullName));

            Info.Where.Add(where.Info.Where);

            return this;
        }

        internal CommandBuilder GetCommand(bool leaveOpen = false)
        {
            ValidateReadonly();

            var cmd = Manager.GetCommand(Config.Translation, leaveOpen);
            cmd.AddCancellationToken(Token);

            if (CommandTimeout > 0)
                cmd.Timeout = CommandTimeout;

            cmd.LogQuery = true;

            return cmd;
        }

        protected void ValidateReadonly()
        {
            if (Manager is null)
                throw new InvalidOperationException(Messages.Query.ReadOnly);
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
            return GetGrammar().Select(false);
        }

        protected internal Grammar GetGrammar()
        {
            return Info.Config.NewGrammar(this);
        }

        #region IDisposed

        ~Query()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
                _lastOpenReader?.Dispose();

            Manager?.CloseByDisposeChild();
            _lastOpenReader = null;
        }

        /// <summary>
        /// Releases all resources used by the object.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has already been _disposed.</exception>
        public void Dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
