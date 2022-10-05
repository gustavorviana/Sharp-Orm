using SharpOrm.Builder;
using System;
using System.Data.Common;

namespace SharpOrm
{
    public class Query : QueryBase, ICloneable
    {
        #region Fields\Properties
        protected readonly DbConnection connection;
        protected virtual Grammar Grammar { get; }
        #endregion

        #region Query

        public Query(DbConnection connection, string table, string alias = "") : this(connection, new Grammar(), table, alias)
        {
        }

        public Query(DbTransaction transaction, string table, string alias = "") : this(transaction, new Grammar(), table, alias)
        {
        }

        public Query(DbConnection connection, Grammar grammar, string table, string alias = "") : base(connection)
        {
            this.connection = connection;
            this.Grammar = grammar;

            this.info.Alias = alias;
            this.info.From = table;
        }

        public Query(DbTransaction transaction, Grammar grammar, string table, string alias = "") : base(transaction)
        {
            this.connection = transaction.Connection;
            this.Grammar = grammar;

            this.info.Alias = alias;
            this.info.From = table;
        }

        #endregion

        public void Select(params Column[] columns)
        {
            this.info.Select.Clear();
            this.info.Select.AddRange(columns);
        }

        #region Join

        public Query Join(string table, string column1, string operation, string column2)
        {
            JoinQuery join = new JoinQuery(this);
            ApplyTableName(join, table);

            join.WriteWhere(new Column(column1), operation, new Column(column2), "AND");

            this.info.Joins.Add(join);
            return this;
        }

        public Query Join(string table, QueryCallback operation)
        {
            JoinQuery join = new JoinQuery(this);
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
            using (DbCommand cmd = this.Grammar.SelectCommand(this))
                return cmd.ExecuteReader();
        }

        public bool Update(params Cell[] cells)
        {
            using (DbCommand cmd = this.Grammar.UpdateCommand(this, cells))
                return cmd.ExecuteNonQuery() > 0;
        }

        public void Insert(params Cell[] cells)
        {
            using (DbCommand cmd = this.Grammar.InsertCommand(this, cells))
                cmd.ExecuteNonQuery();
        }

        public void BulkInsert(params Row[] rows)
        {
            using (DbCommand cmd = this.Grammar.BulkInsert(this, rows))
                cmd.ExecuteScalar();
        }

        public bool Delete()
        {
            using (DbCommand cmd = this.Grammar.DeledeCommand(this))
                return cmd.ExecuteNonQuery() > 0;
        }

        public int Count()
        {
            using (DbCommand cmd = this.Grammar.SelectCommand(this))
                return (int)cmd.ExecuteScalar();
        }

        public object Clone()
        {
            return this.Clone(true);
        }

        public Query Clone(bool withWhere)
        {
            Query query = new Query(this.connection, this.Grammar, this.info.From, this.info.Alias);
            if (withWhere)
                query.info.LoadFrom(this.info);

            return query;
        }
    }
}
