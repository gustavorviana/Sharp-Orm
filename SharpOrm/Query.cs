﻿using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm
{
    public class Query<T> : Query where T : new()
    {
        public static string TableName => Translator.GetTableNameOf(typeof(T));
        protected internal ObjectLoader Loader => Translator.GetLoader(typeof(T));

        #region Query
        public Query(string alias) : base($"{TableName} {alias}")
        {
            QueryExtension.ValidateTranslator();
        }

        public Query() : base(TableName)
        {

        }

        public Query(ConnectionCreator creator, string alias = "") : base(creator, $"{TableName} {alias}")
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(DbConnection connection, string alias = "") : base(connection, $"{TableName} {alias}")
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(DbTransaction transaction, string alias = "") : base(transaction, $"{TableName} {alias}")
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(DbConnection connection, IQueryConfig config) : base(connection, config, TableName)
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(DbTransaction transaction, IQueryConfig config, string alias = "") :
            base(transaction, config, new TableName(TableName, alias))
        {
            QueryExtension.ValidateTranslator();
        }
        #endregion

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
            return this.OnlyFirstSelection(this.GetEnumerable<T>().FirstOrDefault);
        }

        /// <summary>
        /// Search a row in the database by the primary key.
        /// </summary>
        /// <param name="primaryKeys"></param>
        /// <returns></returns>
        public T Find(object primaryKey)
        {
            string pkColumn = this.GetAndValidatePrimaryKey(primaryKey).First();
            using (var query = (Query<T>)this.Clone(false))
            {
                query.Where(pkColumn, primaryKey);
                return query.FirstOrDefault();
            }
        }

        private IEnumerable<string> GetAndValidatePrimaryKey(params object[] pksToCheck)
        {
            if ((pksToCheck?.Length ?? 0) == 0)
                throw new ArgumentNullException(nameof(pksToCheck));

            var properties = this.Loader.GetAttrPrimaryKeys().ToArray();
            if (properties.Length == 0)
                throw new DatabaseException("No primary key has been configured in the model.");

            if (properties.Length != pksToCheck.Length)
                throw new ArgumentException("The number of inserted values ​​is not the same number of primary keys.", nameof(pksToCheck));

            for (int i = 0; i < properties.Length; i++)
                if (properties[i].PropertyType != pksToCheck[i].GetType())
                    throw new InvalidCastException("Inserted type is not the same as defined in the primary key column of the model.");

            return properties.Select(pk => ObjectLoader.GetColumnName(pk));
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
        public int Insert(T obj)
        {
            return this.Insert(Translator.ToRow(obj, typeof(T)).Cells);
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public void BulkInsert(params T[] objs)
        {
            this.BulkInsert(objs.Select(obj => Translator.ToRow(obj, typeof(T))).ToArray());
        }

        /// <summary>
        /// Update table columns using object values.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="columns">If filled in, only inserted columns will be updated.</param>
        /// <returns></returns>
        public int Update(T obj, params string[] columns)
        {
            if (columns.Length == 0)
                return base.Update(this.Loader.GetCells(obj, true).ToArray());

            var cells = this.Loader
                .GetCells(obj)
                .Where(c => columns.Contains(c.Name, StringComparison.OrdinalIgnoreCase));

            var toUpdate = cells.ToArray();
            if (toUpdate.Length == 0)
                throw new InvalidOperationException("Columns inserted to be updated were not found.");

            return base.Update(toUpdate);
        }

        public Query Join<C>(string alias, string column1, string operation, string column2, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.Table = new TableName(Translator.GetTableNameOf(typeof(C)), alias);
            join.WhereColumn(column1, operation, column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join<C>(string alias, QueryCallback operation, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.Table = new TableName(Translator.GetTableNameOf(typeof(C)), alias);

            operation(join);
            this.Info.Joins.Add(join);
            return this;
        }

        public override Query Clone(bool withWhere)
        {
            Query query = this.Transaction == null ?
                new Query<T>(this.Creator, this.Info.Alias) :
                 new Query<T>(this.Transaction, this.Info.Config, this.Info.Alias);

            if (withWhere)
                query.Info.LoadFrom(this.Info);

            this.OnClone(query);

            return query;
        }

        public QueryBase WhereObj(Expression<Func<T, bool>> check)
        {
            return Where(SqlLambdaVisitor.ParseLambda(this.Info, check));
        }
    }

    public class Query : QueryBase, ICloneable
    {
        #region Properties
        public static IObjectTranslator Translator { get; set; } = new ObjectTranslator(new TranslationConfig());

        public bool Distinct { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        private ConnectionCreator _creator;
        public ConnectionCreator Creator => this._creator ?? ConnectionCreator.Default;
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }
        #endregion

        #region Query

        [Obsolete]
        public Query(string table, string alias) : this(ConnectionCreator.Default, table, alias)
        {

        }

        /// <summary>
        /// Creates a new instance of SharpOrm.Query using the default values ​​defined in SharpOrm.QueryDefaults.Default.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        public Query(string table) : this(ConnectionCreator.Default, table)
        {

        }

        [Obsolete]
        public Query(ConnectionCreator creator, string table, string alias)
            : this(creator?.GetConnection() ?? throw new ArgumentNullException(nameof(creator), "Um criador de conexão deve ser foi definido"), creator.Config, new TableName(table, alias))
        {
            this._creator = creator;
        }

        public Query(ConnectionCreator creator, string table)
            : this(creator?.GetConnection() ?? throw new ArgumentNullException(nameof(creator), "Um criador de conexão deve ser foi definido"), creator.Config, new TableName(table))
        {
            this._creator = creator;
        }

        public Query(DbConnection connection, string table) : this(connection, ConnectionCreator.Default.Config, new TableName(table))
        {
        }

        [Obsolete]
        public Query(DbTransaction transaction, string table, string alias) : this(transaction, ConnectionCreator.Default.Config, new TableName(table, alias))
        {

        }

        public Query(DbTransaction transaction, string table) : this(transaction, ConnectionCreator.Default.Config, new TableName(table))
        {

        }

        [Obsolete]
        public Query(DbConnection connection, IQueryConfig config, string table, string alias) : this(connection, config, new TableName(table, alias))
        {

        }

        public Query(DbConnection connection, IQueryConfig config, string table) : this(connection, config, new TableName(table))
        {
        }

        protected Query(DbConnection connection, IQueryConfig config, TableName table) : base(config)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.Info.Table = table;

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();
            }
            catch (Exception ex)
            {
                throw new DbConnectionException(ex);
            }
        }

        [Obsolete]
        public Query(DbTransaction transaction, IQueryConfig config, string table, string alias) : this(transaction, config, new TableName(table, alias))
        {
        }

        public Query(DbTransaction transaction, IQueryConfig config, string table) : this(transaction, config, new TableName(table))
        {

        }

        protected Query(DbTransaction transaction, IQueryConfig config, TableName table) : base(config)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.Connection = transaction.Connection;
            this.Info.Table = table;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Select columns of table by name.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public Query Select(params string[] columnNames)
        {
            if ((columnNames?.Length ?? 0) == 0)
                throw new ArgumentNullException(nameof(columnNames), "At least one column must be inserted.");

            if (columnNames.Length == 1 && columnNames[0] == "*")
                return this.Select((Column)"*");

            return this.Select(columnNames.Select(name => new Column(name)).ToArray());
        }

        /// <summary>
        /// Select column of table by Column object.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Query Select(params Column[] columns)
        {
            if ((columns?.Length ?? 0) == 0)
                throw new ArgumentNullException(nameof(columns), "At least one column must be inserted.");

            this.Info.Select = columns;

            return this;
        }

        #endregion

        #region Join

        public Query Join(string table, string column1, string operation, string column2, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.Table = new TableName(table);
            join.WhereColumn(column1, operation, column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join(string table, QueryCallback operation, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.Table = new TableName(table);
            operation(join);
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

        #region OrderBy

        /// <summary>
        /// Applies an ascending sort.
        /// </summary>
        /// <param name="column">Column that must be ordered.</param>
        /// <returns></returns>
        public Query OrderBy(string column)
        {
            return this.OrderBy(new Column(column), SharpOrm.OrderBy.Asc);
        }

        /// <summary>
        /// Applies descending sort.
        /// </summary>
        /// <param name="column">Column that must be ordered.</param>
        /// <returns></returns>
        public Query OrderByDesc(string column)
        {
            return this.OrderBy(new Column(column), SharpOrm.OrderBy.Desc);
        }

        /// <summary>
        /// Signals which field should be ordered and sorting.
        /// </summary>
        /// <param name="column">Column that must be ordered.</param>
        /// <param name="order">Field ordering.</param>
        /// <returns></returns>
        public Query OrderBy(Column column, OrderBy order)
        {
            return this.OrderBy(new ColumnOrder(column, order));
        }

        /// <summary>
        /// Signals which fields should be ordered and sorting.
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public Query OrderBy(params ColumnOrder[] orders)
        {
            this.Info.Orders = orders;

            return this;
        }

        #endregion

        /// <summary>
        /// Execute SQL Select command and return DataReader.
        /// </summary>
        /// <returns></returns>
        public DbDataReader Execute()
        {
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Select())
                return cmd.ExecuteReader();
        }

        #region DML SQL commands
        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public int Update(params Cell[] cells)
        {
            this.CheckIsSafeOperation();

            if (cells.Length == 0)
                throw new InvalidOperationException("At least one column must be entered.");

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Update(cells))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        public int Insert(params Cell[] cells)
        {
            if (cells.Length == 0)
                throw new InvalidOperationException("At least one column must be entered.");

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Insert(cells))
            {
                object result = cmd.ExecuteScalar();
                return result is DBNull ? 0 : Convert.ToInt32(result);
            }
        }

        /// <summary>
        /// Insert a lot of values ​​using the result of a table (select command);
        /// </summary>
        /// <param name="query"></param>
        /// <param name="columnNames"></param>
        public void Insert(Query query, params string[] columnNames)
        {
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.InsertQuery(query, columnNames))
                cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public void BulkInsert(params Row[] rows)
        {
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.BulkInsert(rows))
                cmd.ExecuteScalar();
        }

        /// <summary>
        /// Removes rows from database
        /// </summary>
        /// <returns></returns>
        public int Delete()
        {
            this.CheckIsSafeOperation();

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Delete())
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Count())
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        public IEnumerable<T> GetEnumerable<T>() where T : new()
        {
            QueryExtension.ValidateTranslator();

            using (var reader = this.Execute())
                while (reader.Read())
                    yield return Translator.ParseFromReader<T>(reader);
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
            return this.OnlyFirstSelection(this.GetEnumerable<Row>().FirstOrDefault);
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
            Query query = this.Transaction == null ? new Query(this.Creator.GetConnection(), this.Info.Config, this.Info.Table) : new Query(this.Transaction, this.Info.Config, this.Info.Table);

            query._creator = this.Creator;

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

        /// <summary>
        /// Signals to the class that code must be executed for the first item and that upon execution, the boundary and offset must be reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        internal T OnlyFirstSelection<T>(Func<T> action)
        {
            int? lastLimit = this.Limit;
            int? lastOffset = this.Offset;

            this.Limit = null;
            this.Offset = null;
            try
            {
                return action();
            }
            finally
            {
                this.Limit = lastLimit;
                this.Offset = lastOffset;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.Transaction == null)
                this.Creator.SafeDisposeConnection(this.Connection);
        }

        public override string ToString()
        {
            using (var grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Select(false))
                return cmd.CommandText;
        }
    }
}
