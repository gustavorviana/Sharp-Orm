using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm
{
    public class Query<T> : Query where T : new()
    {
        public static string TableName => TableReaderBase.GetTableNameOf(typeof(T));
        protected internal TableInfo TableInfo => TableReaderBase.GetTable(typeof(T));
        private string[] foreignsTables = null;
        private int foreignsDepth = 0;

        #region Query
        public Query() : base(TableName)
        {

        }

        public Query(string alias) : this(new DbName(TableName, alias))
        {
        }

        public Query(DbName name) : base(ConnectionCreator.Default, name)
        {

        }

        public Query(ConnectionCreator creator, string alias = "") : base(creator, new DbName(TableName, alias))
        {
        }

        public Query(DbConnection connection, string alias = "") : this(connection, ConnectionCreator.Default?.Config, new DbName(TableName, alias))
        {
        }

        public Query(DbTransaction transaction, string alias = "") : this(transaction, ConnectionCreator.Default?.Config, new DbName(TableName, alias))
        {
        }

        public Query(DbConnection connection, IQueryConfig config) : this(connection, config, new DbName(TableName, null))
        {
        }

        public Query(DbTransaction transaction, IQueryConfig config, string alias = "") : this(transaction, config, new DbName(TableName, alias))
        {
        }

        public Query(DbConnection connection, IQueryConfig config, DbName name) : base(connection, config, name)
        {
        }

        public Query(DbTransaction transaction, IQueryConfig config, DbName name) : base(transaction, config, name)
        {
        }
        #endregion

        /// <summary>
        /// Specifies the foreign tables to be included in the query result up to the specified depth.
        /// </summary>
        /// <param name="depth">The depth to which the foreign tables should be included.</param>
        /// <param name="tables">The names of the foreign tables to include.</param>
        /// <returns>The modified Query<T> object with the specified foreign tables included.</returns>
        public Query<T> WithForeigns(int depth, params string[] tables)
        {
            this.foreignsTables = tables;
            this.foreignsDepth = depth;
            return this;
        }

        /// <summary>
        /// Specifies the foreign tables to be included in the query result up to the default depth (10).
        /// </summary>
        /// <param name="tables">The names of the foreign tables to include.</param>
        /// <returns>The modified Query<T> object with the specified foreign tables included.</returns>
        public Query<T> WithForeigns(params string[] tables)
        {
            this.foreignsTables = tables;
            this.foreignsDepth = 10;
            return this;
        }

        /// <summary>
        /// Creates a Pager<T> object for performing pagination on the query result.
        /// </summary>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number.</param>
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
            var translator = new TableReader(this.foreignsTables, this.foreignsDepth);
            List<K> list = new List<K>();

            using (var reader = this.ExecuteReader())
                while (reader.Read())
                    list.Add(translator.ParseFromReader<K>(reader));

            translator.LoadForeignKeys();
            return list;
        }

        /// <summary>
        /// Searches and returns the first occurrence of an object of type T that matches the values of the provided primary keys.
        /// </summary>
        /// <param name="primaryKeysValues">The values of the primary keys to search for.</param>
        /// <returns>The first occurrence of an object of type T that matches the provided primary keys.</returns>
        public T Find(params object[] primaryKeysValues)
        {
            using (var query = (Query<T>)this.Clone(false))
            {
                var pKeys = this.GetAndValidatePrimaryKey(primaryKeysValues);
                for (var i = 0; i < primaryKeysValues.Length; i++)
                    query.Where(pKeys[i], primaryKeysValues[i]);

                return query.FirstOrDefault();
            }
        }

        private string[] GetAndValidatePrimaryKey(object[] pksToCheck)
        {
            if ((pksToCheck?.Length ?? 0) == 0)
                throw new ArgumentNullException(nameof(pksToCheck));

            var columns = this.TableInfo.Columns.Where(c => c.Key).OrderBy(c => c.Order).ToArray();
            if (columns.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (columns.Length != pksToCheck.Length)
                throw new ArgumentException(Messages.InsertValuesMismatch, nameof(pksToCheck));

            for (int i = 0; i < columns.Length; i++)
                if (columns[i].Type != pksToCheck[i]?.GetType())
                    throw new InvalidCastException(Messages.InsertedTypeMismatch);

            return columns.Select(c => c.Name).ToArray();
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
            return this.Insert(TableReaderBase.ToRow(obj, typeof(T)).Cells);
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public void BulkInsert(params T[] objs)
        {
            this.BulkInsert(objs.Select(obj => TableReaderBase.ToRow(obj, typeof(T))).ToArray());
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
                return base.Update(this.TableInfo.GetCells(obj, true).ToArray());

            var cells = this.TableInfo
                .GetCells(obj)
                .Where(c => columns.Contains(c.Name, StringComparison.OrdinalIgnoreCase));

            var toUpdate = cells.ToArray();
            if (toUpdate.Length == 0)
                throw new InvalidOperationException(Messages.ColumnsNotFound);

            return base.Update(toUpdate);
        }

        #region Join
        public Query Join<C>(string alias, string column1, string column2)
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = "INNER" };
            join.Info.TableName = new DbName(TableReaderBase.GetTableNameOf(typeof(C)), alias);
            join.WhereColumn(column1, "=", column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join<C>(string alias, string column1, string operation, string column2, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.TableName = new DbName(TableReaderBase.GetTableNameOf(typeof(C)), alias);
            join.WhereColumn(column1, operation, column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join<C>(string alias, QueryCallback operation, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.TableName = new DbName(TableReaderBase.GetTableNameOf(typeof(C)), alias);

            operation(join);
            this.Info.Joins.Add(join);
            return this;
        }
        #endregion

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
    }

    public class Query : QueryBase, ICloneable
    {
        #region Properties
        public bool Distinct { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        private ConnectionCreator _creator;
        public ConnectionCreator Creator => this._creator ?? ConnectionCreator.Default;
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }
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
            this._creator = creator;
        }

        public Query(ConnectionCreator creator, DbName table)
            : this(creator?.GetConnection() ?? throw new ArgumentNullException(nameof(creator), Messages.MissingCreator), creator.Config, table)
        {

        }

        public Query(DbConnection connection, string table) : this(connection, ConnectionCreator.Default.Config, new DbName(table))
        {
        }

        public Query(DbTransaction transaction, string table) : this(transaction, ConnectionCreator.Default.Config, new DbName(table))
        {

        }

        public Query(DbConnection connection, IQueryConfig config, string table) : this(connection, config, new DbName(table))
        {
        }

        public Query(DbConnection connection, IQueryConfig config, DbName table) : base(config)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.Info.TableName = table;

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

        public Query(DbTransaction transaction, IQueryConfig config, string table) : this(transaction, config, new DbName(table))
        {

        }

        public Query(DbTransaction transaction, IQueryConfig config, DbName name) : base(config)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.Connection = transaction.Connection;
            this.Info.TableName = name;

            try
            {
                if (transaction.Connection.State != System.Data.ConnectionState.Open)
                    transaction.Connection.Open();
            }
            catch (Exception ex)
            {
                throw new DbConnectionException(ex);
            }
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
                throw new ArgumentNullException(nameof(columnNames), Messages.NoColumnsInserted);

            if (columnNames.Length == 1 && columnNames[0] == "*")
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
            if ((columns?.Length ?? 0) == 0)
                throw new ArgumentNullException(nameof(columns), Messages.NoColumnsInserted);

            this.Info.Select = columns;

            return this;
        }

        #endregion

        #region Join

        public Query Join(string table, string column1, string column2)
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = "INNER" };
            join.Info.TableName = new DbName(table);
            join.WhereColumn(column1, "=", column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join(string table, string column1, string operation, string column2, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.TableName = new DbName(table);
            join.WhereColumn(column1, operation, column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join(string table, QueryCallback operation, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.TableName = new DbName(table);
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
        public DbDataReader ExecuteReader()
        {
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Select())
                return cmd.ExecuteReader();
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public T[] ExecuteArrayScalar<T>()
        {
            List<T> list = new List<T>();

            using (var reader = this.ExecuteReader())
                while (reader.Read())
                    list.Add((T)TableReaderBase.Registry.FromSql(reader[0], typeof(T)));

            return list.ToArray();
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public T ExecuteScalar<T>()
        {
            return (T)TableReaderBase.Registry.FromSql(this.ExecuteScalar(), typeof(T));
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public object ExecuteScalar()
        {
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Select())
                return cmd.ExecuteScalar();
        }

        #region DML SQL commands

        public int Update(Dictionary<string, object> cells)
        {
            return this.Update(cells.Select(x => new Cell(x.Key, x.Value)).ToArray());
        }

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public int Update(params Cell[] cells)
        {
            this.CheckIsSafeOperation();

            if (cells.Length == 0)
                throw new InvalidOperationException(Messages.NoColumnsInserted);

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Update(cells))
                return cmd.ExecuteNonQuery();
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
            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Count(column))
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        public virtual IEnumerable<T> GetEnumerable<T>() where T : new()
        {
            var translator = new TableReader();
            using (var reader = this.ExecuteReader())
                while (reader.Read())
                    yield return translator.ParseFromReader<T>(reader);
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
