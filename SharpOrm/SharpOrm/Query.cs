using SharpOrm.Builder;
using SharpOrm.Errors;
using System;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    public class Query : QueryBase, ICloneable
    {
        #region Properties

        /// <summary>
        /// Configurations for query
        /// </summary>
        protected IQueryConfig Config { get; }

        public bool Distinct { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; set; }

        #endregion

        #region Query

        /// <summary>
        /// Creates a new instance of SharpOrm.Query using the default values ​​defined in SharpOrm.QueryDefaults.
        /// </summary>
        /// <param name="table">Name of the table to be used.</param>
        /// <param name="alias">Table alias.</param>
        public Query(string table, string alias = "") : this(QueryDefaults.Connection, QueryDefaults.Config, table, alias)
        {

        }

        public Query(DbConnection connection, string table, string alias = "") : this(connection, QueryDefaults.Config, table, alias)
        {
        }

        public Query(DbConnection connection, IQueryConfig config, string table, string alias = "")
        {
            if (string.IsNullOrEmpty(table))
                throw new ArgumentNullException(nameof(table));

            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.Config = config ?? throw new ArgumentNullException(nameof(config));

            this.info.Alias = alias;
            this.info.From = table;

            this.info.ColumnPrefix = config.ColumnPrefix;
            this.info.ColumnSuffix = config.ColumnSuffix;
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
            return this.Select(columnNames.Select(name => new Column(name)).ToArray());
        }

        /// <summary>
        /// Select column of table by Column object.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Query Select(params Column[] columns)
        {
            this.info.Select.Clear();
            this.info.Select.AddRange(columns);

            return this;
        }

        #endregion

        #region Join

        public Query Join(string table, string column1, string operation, string column2, string type = "INNER")
        {
            JoinQuery join = new JoinQuery { Type = type };
            ApplyTableName(join, table);

            join.WhereColumn(column1, operation, column2);

            this.info.Joins.Add(join);
            return this;
        }

        public Query Join(string table, QueryCallback operation, string type = "INNER")
        {
            JoinQuery join = new JoinQuery
            {
                Type = type
            };
            ApplyTableName(join, table);
            operation(join);
            this.info.Joins.Add(join);
            return this;
        }

        private void ApplyTableName(QueryBase query, string fullName)
        {
            if (!fullName.Contains(" "))
            {
                query.info.From = fullName;
                return;
            }

            var splits = fullName.Split(' ');
            if (splits.Length > 3)
                throw new ArgumentException("O nome da tabela é inválido");

            query.info.From = splits[0];

            if (splits.Length == 2)
            {
                query.info.Alias = splits[1];
                return;
            }

            if (splits[2].ToLower() != "as")
                throw new ArgumentException("O nome da tabela é inválido");

            query.info.Alias = splits[2];
        }

        #endregion

        #region GroupBy

        public Query GroupBy(params string[] columnNames)
        {
            return this.GroupBy(columnNames.Select(name => new Column(name)).ToArray());
        }

        public Query GroupBy(params Column[] columns)
        {
            this.info.GroupsBy.Clear();
            this.info.GroupsBy.AddRange(columns);

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
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetSelectCommand())
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

            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetUpdateCommand(cells))
                return cmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        public void Insert(params Cell[] cells)
        {
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetInsertCommand(cells))
                cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public void BulkInsert(params Row[] rows)
        {
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetBulkInsertCommand(rows))
                cmd.ExecuteScalar();
        }

        /// <summary>
        /// Removes rows from database
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            this.CheckIsSafeOperation();

            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetDeleteCommand())
                return cmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Counts the amount of results available. 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            using (Grammar grammar = this.Config.NewGrammar(this.Clone(true).Select(Column.CountAll)))
            using (DbCommand cmd = grammar.GetSelectCommand())
                return Convert.ToInt64(cmd.ExecuteScalar());
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
            Query query = new Query(this.Connection, this.Config, this.info.From, this.info.Alias);
            if (withWhere)
                query.info.LoadFrom(this.info);

            return query;
        }

        /// <summary>
        /// Throws an error if only operations with "WHERE" are allowed and there are none configured.
        /// </summary>
        protected void CheckIsSafeOperation()
        {
            if (this.Config.OnlySafeModifications && this.info.Wheres.Length == 0)
                throw new UnsafeDbOperation();
        }
        #endregion

        public override string ToString()
        {
            using (var grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetSelectCommand(false))
                return cmd.CommandText;
        }
    }
}
