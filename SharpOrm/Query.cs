﻿using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.Builder.Grammars;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm
{
    /// <summary>
    /// Class responsible for interacting with the data of a database table.
    /// </summary>
    /// <typeparam name="T">Type that should be used to interact with the table.</typeparam>
    public class Query<T> : Query
    {
        private ObjectReader _objReader;

        protected internal TableInfo TableInfo { get; }
        private List<MemberInfo> _fkToLoad = new List<MemberInfo>();

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
            ((IRootTypeMap)Info).RootType = typeof(T);
            TableInfo = manager.Config.Translation.GetTable(((IRootTypeMap)Info).RootType);
            ValidateModelOnSave = manager.Config.ValidateModelOnSave;
            ApplyValidations();

            if (TableInfo.SoftDelete != null)
                Trashed = Trashed.Except;
        }

        private Query(DbName table, QueryConfig config) : base(table, config)
        {
            ((IRootTypeMap)Info).RootType = typeof(T);
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
            var processor = new ExpressionProcessor<T>(Info, config);
            return processor.ParseColumns(expression).ToArray();
        }

        #region AddForeign

        /// <summary>
        /// Adds a foreign key to the query based on the specified column expression.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the query.</typeparam>
        /// <param name="call">An expression representing the column to be added as a foreign key.</param>
        /// <returns>The query with the added foreign key.</returns>
        public Query<T> AddForeign(Expression<ColumnExpression<T>> call)
        {
            foreach (var column in ExpressionUtils<T>.GetMemberPath(call, false).Reverse())
                if (!_fkToLoad.Contains(column))
                    _fkToLoad.Add(column);

            return this;
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
        public override Task<int> DeleteAsync()
        {
            return TaskUtils.Async(Delete);
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
        public Task<int> DeleteAsync(bool force)
        {
            return TaskUtils.Async(() => Delete(force));
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
        public Task<int> RestoreAsync()
        {
            return TaskUtils.Async(Restore);
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
        public Task<T> FirstOrDefaultAsync()
        {
            return TaskUtils.Async(FirstOrDefault);
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
            var enumerable = (DbCommandEnumerable<K>)base.GetEnumerable<K>();
            if (TranslationUtils.IsNullOrEmpty(_fkToLoad))
                return enumerable;

            try
            {
                FkLoaders fkLoaders = new FkLoaders(Manager, _fkToLoad, Token);

                enumerable._fkQueue = fkLoaders;
                var list = enumerable.ToList();
                fkLoaders.LoadForeigns();

                return list;
            }
            finally
            {
                Manager?.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Asynchronously searches and returns the first occurrence of an object of type T that matches the values of the provided primary keys.
        /// </summary>
        /// <param name="primaryKeysValues">The values of the primary keys to search for.</param>
        /// <returns>A task representing the asynchronous operation, with the first occurrence of an object of type T that matches the provided primary keys.</returns>
        public Task<T> FindAsync(params object[] primaryKeysValues)
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
        public async Task<T[]> GetAsync()
        {
            return await TaskUtils.Async(Get);
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
        public async Task<int> InsertAsync(T obj)
        {
            return await TaskUtils.Async(() => Insert(obj));
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Id of row.</returns>
        public int Insert(T obj)
        {
            ValidateReadonly();

            var reader = GetObjectReader(true, true);
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
        public async Task<int> BulkInsertAsync(params T[] objs)
        {
            return await TaskUtils.Async(() => BulkInsert(objs));
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
        public async Task<int> BulkInsertAsync(IEnumerable<T> objs)
        {
            return await TaskUtils.Async(() => BulkInsert(objs));
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(IEnumerable<T> objs)
        {
            var reader = GetObjectReader(true, true);
            return base.BulkInsert(objs.Select(x => reader.ReadRow(x)));
        }

        /// <summary>
        /// Asynchronously updates table keys using object values.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        /// <returns>A task representing the asynchronous operation, with the number of updated rows.</returns>
        public async Task<int> UpdateAsync(T obj)
        {
            return await TaskUtils.Async(() => Update(obj));
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

            return base.Update(GetObjectReader(false, false).ReadCells(obj));
        }

        /// <summary>
        /// Asynchronously updates table keys using object values.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        /// <param name="expression">Expression to retrieve the properties that should be saved.</param>
        /// <returns>A task representing the asynchronous operation, with the number of updated rows.</returns>
        public async Task<int> UpdateAsync(T obj, Expression<ColumnExpression<T>> expression)
        {
            return await TaskUtils.Async(() => Update(obj, expression));
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

            return Update(GetObjectReader(false, false).Only(expression).ReadCells(obj));
        }

        /// <summary>
        /// Asynchronously updates table keys using object values.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        /// <param name="columns">The columns to update.</param>
        /// <returns>A task representing the asynchronous operation, with the number of updated rows.</returns>
        public async Task<int> UpdateAsync(T obj, params string[] columns)
        {
            return await TaskUtils.Async(() => Update(obj, columns));
        }

        /// <summary>
        /// Update table keys using object values.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="columns">Update table keys using object values..</param>
        /// <returns></returns>
        public int Update(T obj, params string[] columns)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (columns.Length == 0)
                throw new ArgumentNullException(nameof(columns));

            var reader = GetObjectReader(false, false);
            if (columns.Length > 0) reader.Only(columns);

            var toUpdate = reader.ReadCells(obj).ToArray();
            if (toUpdate.Length == 0)
                throw new InvalidOperationException(Messages.ColumnsNotFound);

            return base.Update(toUpdate);
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists. If null, all columns will be updated.</param>
        public async Task UpsertAsync(T obj, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp = null)
        {
            await TaskUtils.Async(() => Upsert(obj, toCheckColumnsExp, updateColumnsExp));
        }

        /// <summary>
        /// Inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumnsExp">The columns to check for existing records.</param>
        /// <param name="updateColumnsExp">The columns to update if a record exists. If null, all columns will be updated.</param>
        public void Upsert(T obj, Expression<ColumnExpression<T>> toCheckColumnsExp, Expression<ColumnExpression<T>> updateColumnsExp = null)
        {
            var processor = new ExpressionProcessor<T>(Info, ExpressionConfig.New);
            var toCheckColumns = processor.ParseColumnNames(toCheckColumnsExp).ToArray();
            var updateColumns = processor.ParseColumnNames(updateColumnsExp).ToArray();

            Upsert(obj, toCheckColumns, updateColumns);
        }

        /// <summary>
        /// Asynchronously inserts or updates an object in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="obj">The object to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        public async Task UpsertAsync(T obj, string[] toCheckColumns, params string[] updateColumns)
        {
            await TaskUtils.Async(() => Upsert(obj, toCheckColumns, updateColumns));
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

            base.Upsert(GetObjectReader(true, true).ReadRow(obj), toCheckColumns, updateColumns);
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
            var name = new ExpressionProcessor<T>(Info, ExpressionConfig.None).GetTableName(table, out var member);
            var dbName = new DbName(name, alias);

            if (Info.Joins.Any(j => j.MemberInfo == member && j.Info.TableName == dbName))
                throw new InvalidOperationException(string.Format(Messages.Query.DuplicateJoin, member.Name));

            JoinQuery join = new JoinQuery(Info.Config, dbName, typeof(T)) { Type = type, GrammarOptions = grammarOptions, MemberInfo = member };
            join.Where(GetColumn(join.Info, column1, true), operation, GetColumn(column2, true));
            Info.Joins.Add(join);

            return this;
        }

        /// <summary>
        /// Performs an INNER JOIN between this query and another table with a specified alias.
        /// </summary>
        /// <typeparam name="C">The type of the related table.</typeparam>
        /// <param name="alias">The alias for the table. If no alias is desired, use an empty string.</param>
        /// <param name="column1">The first column expression to compare.</param>
        /// <param name="column2">The second column expression to compare.</param>
        /// <returns>The current query instance.</returns>
        public Query<T> Join<C>(string alias, Expression<ColumnExpression<C>> column1, Expression<ColumnExpression<T>> column2)
        {
            return (Query<T>)Join<C>(alias, column1, "=", column2);
        }

        /// <summary>
        /// Performs a JOIN between this query and another table with a specified alias, operation, and join type.
        /// </summary>
        /// <typeparam name="C">The type of the related table.</typeparam>
        /// <param name="alias">The alias for the table. If no alias is desired, use an empty string.</param>
        /// <param name="column1">The first column expression to compare.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="column2">The second column expression to compare.</param>
        /// <param name="type">The type of join (e.g., "INNER", "LEFT").</param>
        /// <returns>The current query instance.</returns>
        public Query<T> Join<C>(string alias, Expression<ColumnExpression<C>> column1, string operation, Expression<ColumnExpression<T>> column2, string type = "INNER")
        {
            var name = Config.Translation.GetTableName(typeof(C));

            return (Query<T>)Join(new DbName(name, alias), q =>
            {
                q.Where(GetColumn(q.Info, column1, true), operation, GetColumn(column2, true));
            }, type);
        }

        public Query<T> Join<R>(string alias, string column1, string column2)
        {
            var dbName = DbName.Of<T>(alias, Config.Translation);
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

        private ExpressionColumn GetColumn(Expression<ColumnExpression<T>> column, bool forceTablePrefix = false)
        {
            return GetColumn(Info, column, forceTablePrefix);
        }

        private static ExpressionColumn GetColumn<K>(IReadonlyQueryInfo info, Expression<ColumnExpression<K>> column, bool forceTablePrefix)
        {
            var processor = new ExpressionProcessor<K>(info, ExpressionConfig.SubMembers | ExpressionConfig.Method)
            {
                ForceTablePrefix = forceTablePrefix,
            };

            return processor.ParseColumns(column).First();
        }

        private Column GetColumn<K>(Expression<ColumnExpression<T, K>> column)
        {
            var processor = new ExpressionProcessor<T>(Info, ExpressionConfig.All);
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

            query._fkToLoad.AddRange(_fkToLoad);
        }

        internal ObjectReader GetObjectReader(bool readPk, bool isCreate)
        {
            if (_objReader == null)
            {
                _objReader = new ObjectReader(TableInfo);
                _objReader.ReadFk = Info.Config.LoadForeign;
                _objReader.Validate = true;
            }

            _objReader.IgnoreTimestamps = IgnoreTimestamps;
            _objReader.IsCreate = isCreate;
            _objReader.ReadPk = readPk;

            return _objReader;
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
        /// Gets a value indicating whether the object has been disposed.
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
        public Task<int> UpsertAsync(DbName sourceName, string[] toCheckColumns, string[] updateColumns, string[] insertColumns)
        {
            return TaskUtils.Async(() => Upsert(sourceName, toCheckColumns, updateColumns, insertColumns));
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
            if (toCheckColumns == null || toCheckColumns.Length == 0)
                throw new ArgumentNullException(nameof(toCheckColumns), "ToCheckColumns cannot be null or empty.");

            if (updateColumns == null || updateColumns.Length == 0)
                throw new ArgumentNullException(nameof(updateColumns), "UpdateColumns cannot be null or empty.");

            if (insertColumns == null || insertColumns.Length == 0)
                throw new ArgumentNullException(nameof(insertColumns), "InsertColumns cannot be null or empty.");

            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().Upsert(sourceName, toCheckColumns, updateColumns, insertColumns)) +
                    cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Asynchronously inserts or updates a row in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="row">The row to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task UpsertAsync(Row row, string[] toCheckColumns, string[] updateColumns = null)
        {
            return TaskUtils.Async(() => Upsert(row, toCheckColumns, updateColumns));
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

            if (toCheckColumns == null || toCheckColumns.Length == 0)
                throw new ArgumentNullException(nameof(toCheckColumns), "ToCheckColumns cannot be null or empty.");

            ValidateReadonly();

            if (Info.Config.NativeUpsertRows)
                Upsert(new Row[] { row }, toCheckColumns, updateColumns);
            else
                NonNativeUpsert(row, toCheckColumns, updateColumns);
        }

        /// <summary>
        /// Asynchronously inserts or updates multiple rows in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="rows">The rows to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <returns>A task representing the asynchronous operation, with the number of affected rows.</returns>
        public Task<int> UpsertAsync(Row[] rows, string[] toCheckColumns, string[] updateColumns = null)
        {
            return TaskUtils.Async(() => Upsert(rows, toCheckColumns, updateColumns));
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
            if (rows == null || rows.Length == 0)
                throw new ArgumentNullException(nameof(rows), "Rows cannot be null or empty.");

            if (toCheckColumns == null || toCheckColumns.Length == 0)
                throw new ArgumentNullException(nameof(toCheckColumns), "ToCheckColumns cannot be null or empty.");

            ValidateReadonly();

            if (updateColumns == null || updateColumns.Length == 0)
                updateColumns = rows[0].Cells.Select(x => x.Name).Where(x => !toCheckColumns.Contains(x)).ToArray();

            if (!Config.NativeUpsertRows) return NonNativeUpsertRows(rows, toCheckColumns, updateColumns);
            else
                using (var cmd = GetCommand())
                    return cmd.SetExpressionWithAffectedRows(GetGrammar().Upsert(rows, toCheckColumns, updateColumns)) + cmd.ExecuteNonQuery();
        }

        internal int NonNativeUpsertRows(Row[] rows, string[] toCheckColumns, string[] updateColumns)
        {
            foreach (var row in rows)
                NonNativeUpsert(row, toCheckColumns, updateColumns);

            return 0;
        }

        private void NonNativeUpsert(Row row, string[] toCheckColumns, params string[] updateColumns)
        {
            ValidateReadonly();

            using (var query = Clone(false))
            {
                foreach (var column in toCheckColumns)
                    query.Where(column, row[column]);

                if (query.Any()) query.Update(row.Cells.Where(x => AnyColumn(updateColumns, x.Name)));
                else query.Insert(row.Cells);
            }
        }

        private static bool AnyColumn(string[] columns, string column)
        {
            if (columns == null || columns.Length == 0) return true;

            for (int i = 0; i < columns.Length; i++)
                if (columns[i].Equals(column, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        /// <summary>
        /// Asynchronously updates rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(params Cell[] cells)
        {
            ValidateReadonly();
            if (!cells.Any())
                throw new InvalidOperationException(Messages.NoColumnsInserted);

            return await UpdateAsync((IEnumerable<Cell>)cells);
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
        public async Task<int> UpdateAsync(IEnumerable<Cell> cells)
        {
            ValidateReadonly();
            CheckIsSafeOperation();

            using (var cmd = GetCommand())
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
        public async Task<int> InsertAsync(params Cell[] cells)
        {
            ValidateReadonly();

            if (cells.Length == 0)
                throw new InvalidOperationException(Messages.AtLeastOneColumnRequired);

            return TranslationUtils.TryNumeric(await InsertAsync(cells, true));
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
        public async Task<int> InsertAsync(IEnumerable<Cell> cells)
        {
            ValidateReadonly();

            return TranslationUtils.TryNumeric(await InsertAsync(cells, true));
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
        public async Task<int> InsertAsync(QueryBase queryBase, params string[] columnNames)
        {
            using (var cmd = await GetCommand().SetExpressionAsync(GetGrammar().InsertQuery(queryBase, columnNames)))
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
                throw new InvalidOperationException("");

            using (var cmd = GetCommand().SetExpression(GetGrammar().InsertExpression(expression, columnNames)))
                return cmd.ExecuteNonQuery();
        }

        internal async Task<object> InsertAsync(IEnumerable<Cell> cells, bool returnsInsetionId)
        {
            ValidateReadonly();

            using (var cmd = GetCommand())
            {
                cmd.SetExpression(GetGrammar().Insert(cells, returnsInsetionId));
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
        public async Task<int> BulkInsertAsync(params Row[] rows)
        {
            return await BulkInsertAsync((ICollection<Row>)rows);
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
        public async Task<int> BulkInsertAsync(IEnumerable<Row> rows)
        {
            using (var cmd = GetCommand())
                return await cmd.SetExpressionWithAffectedRowsAsync(GetGrammar().BulkInsert(rows)) +
                    await cmd.ExecuteWithRecordsAffectedAsync();
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public int BulkInsert(IEnumerable<Row> rows)
        {
            using (var cmd = GetCommand())
                return cmd.SetExpressionWithAffectedRows(GetGrammar().BulkInsert(rows)) + cmd.ExecuteWithRecordsAffected();
        }

        /// <summary>
        /// Asynchronously removes rows from database
        /// </summary>
        /// <returns>Number of deleted rows.</returns>
        public virtual async Task<int> DeleteAsync()
        {
            ValidateReadonly();
            CheckIsSafeOperation();

            using (var cmd = GetCommand())
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
        public Task<int> DeleteIncludingJoinsAsync(params string[] tables)
        {
            return TaskUtils.Async(() => DeleteIncludingJoins(tables));
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
        public async Task<long> CountAsync()
        {
            ValidateReadonly();
            return Convert.ToInt64(await GetCommand().SetExpression(GetGrammar().Count()).ExecuteScalarAsync());
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            return Convert.ToInt64(GetCommand().SetExpression(GetGrammar().Count()).ExecuteScalar());
        }

        /// <summary>
        /// Asynchronously counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public async Task<long> CountAsync(string columnName)
        {
            return await CountAsync(new Column(columnName));
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
        public async Task<long> CountAsync(Column column)
        {
            return Convert.ToInt64(await GetCommand().SetExpression(GetGrammar().Count(column)).ExecuteScalarAsync());
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <param name="column">Column to count.</param>
        /// <returns></returns>
        public long Count(Column column)
        {
            return Convert.ToInt64(GetCommand().SetExpression(GetGrammar().Count(column)).ExecuteScalar());
        }

        /// <summary>
        /// Asynchronously returns all rows of the table
        /// </summary>
        /// <returns></returns>
        public Task<Row[]> ReadRowsAsync()
        {
            return TaskUtils.Async(ReadRows);
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
        public Task<Row> FirstRowAsync()
        {
            return TaskUtils.Async(FirstRow);
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
                return GetEnumerable<Row>().FirstOrDefault();
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
        public Task<T[]> GetAsync<T>()
        {
            return TaskUtils.Async(() => Get<T>());
        }

        /// <summary>
        /// Retrieves an collection of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <returns>An collection of the specified type.</returns>
        public T[] Get<T>()
        {
            return GetEnumerable<T>().ToArray();
        }

        /// <summary>
        /// Retrieves an enumerable collection of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the enumerable collection.</typeparam>
        /// <returns>An enumerable collection of the specified type.</returns>
        public virtual IEnumerable<T> GetEnumerable<T>()
        {
            Token.ThrowIfCancellationRequested();

            return GetCommand(true).SetExpression(Info.Config.NewGrammar(this).Select()).ExecuteEnumerable<T>(true);
        }

        /// <summary>
        /// Asynchronously executes the query and returns the first column of all rows in the result. All other keys are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public async Task<T[]> ExecuteArrayScalarAsync<T>()
        {
            Token.ThrowIfCancellationRequested();

            using (var cmd = GetCommand().SetExpression(GetGrammar().Select()))
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
        public async Task<T> ExecuteScalarAsync<T>()
        {
            using (var cmd = GetCommand().SetExpression(GetGrammar().Select()))
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
        public async Task<object> ExecuteScalarAsync()
        {
            using (var cmd = GetCommand().SetExpression(GetGrammar().Select()))
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
            var query = Query.ReadOnly(table);
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
            var query = Query.ReadOnly(table);
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
            if (where is Query) throw new NotSupportedException($"Cannot add a {where.GetType().FullName} to the WHERE clause.");

            Info.Where.Add(where.Info.Where);

            return this;
        }

        internal CommandBuilder GetCommand(bool leaveOpen = false)
        {
            ValidateReadonly();

            var cmd = Manager.GetCommand(Config.Translation, leaveOpen);
            cmd.SetCancellationToken(Token);

            if (CommandTimeout > 0)
                cmd.Timeout = CommandTimeout;

            cmd.LogQuery = true;

            return cmd;
        }

        protected void ValidateReadonly()
        {
            if (Manager is null)
                throw new InvalidOperationException("This query is read-only.");
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

            if (disposing && _lastOpenReader is CommandBuilder last)
                last.Dispose();

            Manager?.CloseByDisposeChild();
            _lastOpenReader = null;
        }

        /// <summary>
        /// Releases all resources used by the object.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
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
