using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    public class Query<T> : Query where T : new()
    {
        protected internal ObjectLoader Loader => Translator.GetLoader(typeof(T));

        public Query(string alias = "") : base(Translator.GetTableNameOf(typeof(T)), alias)
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(ConnectionCreator creator, string alias = "") : this(creator.GetConnection(), creator.Config, alias)
        {
        }

        public Query(DbConnection connection, string alias = "") : base(connection, Translator.GetTableNameOf(typeof(T)), alias)
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(DbTransaction transaction, string alias = "") : base(transaction, Translator.GetTableNameOf(typeof(T)), alias)
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(DbConnection connection, IQueryConfig config, string alias = "") : base(connection, config, Translator.GetTableNameOf(typeof(T)), alias)
        {
            QueryExtension.ValidateTranslator();
        }

        public Query(DbTransaction transaction, IQueryConfig config, string alias = "") : base(transaction, config, Translator.GetTableNameOf(typeof(T)), alias)
        {
            QueryExtension.ValidateTranslator();
        }

        public Pager<T> Paginate(int peerPage, int currentPage)
        {
            return Pager<T>.FromBuilder(this, peerPage, currentPage);
        }

        /// <summary>
        /// Get first result.
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault()
        {
            return this.TempOnlyFirstSelection(this.ReadResults<T>().FirstOrDefault);
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
            return this.ReadResults<T>().ToArray();
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
        public bool Update(T obj, params string[] columns)
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
            join.Info.From = Translator.GetTableNameOf(typeof(C));
            join.Info.Alias = alias;

            join.WhereColumn(column1, operation, column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join<C>(string alias, QueryCallback operation, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config) { Type = type };
            join.Info.From = Translator.GetTableNameOf(typeof(C));
            join.Info.Alias = alias;

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

            return query;
        }
    }

    public class Query : QueryBase, ICloneable
    {
        #region Properties
        public static IObjectTranslator Translator { get; set; } = new ObjectTranslator(new TranslationConfig());

        public bool Distinct { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        private readonly ConnectionCreator _creator;
        public ConnectionCreator Creator => this._creator ?? ConnectionCreator.Default;
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }
        #endregion

        #region Query

        /// <summary>
        /// Creates a new instance of SharpOrm.Query using the default values ​​defined in SharpOrm.QueryDefaults.Default.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="alias">Table alias.</param>
        public Query(string table, string alias = "") : this(ConnectionCreator.Default, table, alias)
        {

        }

        public Query(ConnectionCreator creator, string table, string alias = "") : this(creator.GetConnection(), creator.Config, table, alias)
        {
            this._creator = creator;
        }

        public Query(DbConnection connection, string table, string alias = "") : this(connection, ConnectionCreator.Default.Config, table, alias)
        {
        }

        public Query(DbTransaction transaction, string table, string alias = "") : this(transaction, ConnectionCreator.Default.Config, table, alias)
        {

        }

        public Query(DbConnection connection, IQueryConfig config, string table, string alias = "") : base(config)
        {
            if (string.IsNullOrEmpty(table))
                throw new ArgumentNullException(nameof(table));

            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            this.Info.Alias = alias;
            this.Info.From = table;
        }

        public Query(DbTransaction transaction, IQueryConfig config, string table, string alias = "") : base(config)
        {
            if (string.IsNullOrEmpty(table))
                throw new ArgumentNullException(nameof(table));

            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.Connection = transaction.Connection;

            this.Info.Alias = alias;
            this.Info.From = table;
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
            ApplyTableName(join, table);

            join.WhereColumn(column1, operation, column2);

            this.Info.Joins.Add(join);
            return this;
        }

        public Query Join(string table, QueryCallback operation, string type = "INNER")
        {
            JoinQuery join = new JoinQuery(this.Info.Config)
            {
                Type = type
            };
            ApplyTableName(join, table);
            operation(join);
            this.Info.Joins.Add(join);
            return this;
        }

        private void ApplyTableName(QueryBase query, string fullName)
        {
            if (!fullName.Contains(" "))
            {
                query.Info.From = fullName;
                return;
            }

            var splits = fullName.Split(' ');
            if (splits.Length > 3)
                throw new ArgumentException("O nome da tabela é inválido");

            query.Info.From = splits[0];

            if (splits.Length == 2)
            {
                query.Info.Alias = splits[1];
                return;
            }

            if (splits[2].ToLower() != "as")
                throw new ArgumentException("O nome da tabela é inválido");

            query.Info.Alias = splits[2];
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

        #region DbCommand\DbDataReader

        /// <summary>
        /// Create DbCommand using connection and transaction.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DbCommand CreateCommand(StringBuilder builder)
        {
            return this.CreateCommand(builder.ToString());
        }

        /// <summary>
        /// Create DbCommand using connection and transaction.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DbCommand CreateCommand(string sql)
        {
            var cmd = this.Connection.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandText = sql;
            return cmd;
        }

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

        #endregion

        #region DML SQL commands
        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public bool Update(params Cell[] cells)
        {
            this.CheckIsSafeOperation();

            if (cells.Length == 0)
                throw new InvalidOperationException("At least one column must be entered.");

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Update(cells))
                return cmd.ExecuteNonQuery() > 0;
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
                if (result is DBNull)
                    return 0;

                return Convert.ToInt32(result);
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
        public bool Delete()
        {
            this.CheckIsSafeOperation();

            using (Grammar grammar = this.Info.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.Delete())
                return cmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            Column[] lastSelect = this.Info.Select;
            try
            {
                using (Grammar grammar = this.Info.Config.NewGrammar(this.Select(Column.CountAll)))
                using (DbCommand cmd = grammar.Select())
                    return Convert.ToInt64(cmd.ExecuteScalar());
            }
            finally
            {
                this.Select(lastSelect);
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
            Query query = this.Transaction == null ?
                new Query(this.Creator, this.Info.From, this.Info.Alias) :
                 new Query(this.Transaction, this.Info.Config, this.Info.From, this.Info.Alias);

            if (withWhere)
                query.Info.LoadFrom(this.Info);

            return query;
        }

        /// <summary>
        /// Throws an error if only operations with "WHERE" are allowed and there are none configured.
        /// </summary>
        protected void CheckIsSafeOperation()
        {
            if (this.Info.Config.OnlySafeModifications && this.Info.Where.Length == 0)
                throw new UnsafeDbOperation();
        }
        #endregion

        /// <summary>
        /// Signals to the class that code must be executed for the first item and that upon execution, the boundary and offset must be reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        internal T TempOnlyFirstSelection<T>(Func<T> action)
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
