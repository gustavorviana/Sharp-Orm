using SharpOrm.Builder;
using System;
using System.Data.Common;
using System.Linq;

namespace SharpOrm
{
    public class Query : QueryBase, ICloneable
    {
        protected IQueryConfig Config { get; }

        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; set; }

        #region Query

        public Query(DbConnection connection, string table, string alias = "") : this(connection, new DefaultQueryConfig(), table, alias)
        {
        }

        public Query(DbTransaction transaction, string table, string alias = "") : this(transaction, new DefaultQueryConfig(), table, alias)
        {
        }

        public Query(DbConnection connection, IQueryConfig config, string table, string alias = "")
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config)); ;

            if (string.IsNullOrEmpty(table))
                throw new ArgumentNullException(nameof(table));

            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.Config = config ?? throw new ArgumentNullException(nameof(config));

            this.info.Alias = alias;
            this.info.From = table;
        }

        public Query(DbTransaction transaction, IQueryConfig config, string table, string alias = "")
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
            this.Connection = transaction.Connection;
            this.Transaction = transaction;

            this.info.Alias = alias;
            this.info.From = table;
        }

        #endregion

        public Query Select(params string[] columns)
        {
            return this.Select(columns.Select(name => new Column(name)).ToArray());
        }

        public Query Select(params Column[] columns)
        {
            this.info.Select.Clear();
            this.info.Select.AddRange(columns);

            return this;
        }

        #region Join

        public Query Join(string table, string column1, string operation, string column2)
        {
            JoinQuery join = new JoinQuery();
            ApplyTableName(join, table);

            join.WriteWhere(new Column(column1), operation, new Column(column2), "AND");

            this.info.Joins.Add(join);
            return this;
        }

        public Query Join(string table, QueryCallback operation)
        {
            JoinQuery join = new JoinQuery();
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

        public DbDataReader ExecuteReader()
        {
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetSelectCommand())
                return cmd.ExecuteReader();
        }

        public bool Update(params Cell[] cells)
        {
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetUpdateCommand(cells))
                return cmd.ExecuteNonQuery() > 0;
        }

        public void Insert(params Cell[] cells)
        {
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetInsertCommand(cells))
                cmd.ExecuteNonQuery();
        }

        public void BulkInsert(params Row[] rows)
        {
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetBulkInsertCommand(rows))
                cmd.ExecuteScalar();
        }

        public bool Delete()
        {
            using (Grammar grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetDeleteCommand())
                return cmd.ExecuteNonQuery() > 0;
        }

        public long Count()
        {
            using (Grammar grammar = this.Config.NewGrammar(this.Clone(true).Select(Column.CountAll)))
            using (DbCommand cmd = grammar.GetSelectCommand())
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        public object Clone()
        {
            return this.Clone(true);
        }

        public Query Clone(bool withWhere)
        {
            Query query = new Query(this.Connection, this.Config, this.info.From, this.info.Alias);
            if (withWhere)
                query.info.LoadFrom(this.info);

            return query;
        }

        public override string ToString()
        {
            using (var grammar = this.Config.NewGrammar(this))
            using (DbCommand cmd = grammar.GetSelectCommand(false))
                return cmd.CommandText;
        }
    }
}
