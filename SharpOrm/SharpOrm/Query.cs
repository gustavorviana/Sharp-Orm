using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace SharpOrm
{
    public class Query : QueryBase
    {
        #region Fields\Properties
        protected readonly DbConnection connection;
        protected virtual Grammar Grammar { get; } = new Grammar();
        #endregion

        #region Query

        public Query(DbConnection connection, string table) : base(connection)
        {
            this.connection = connection;
            this.From = table;
        }

        public Query(DbTransaction transaction, string table) : base(transaction)
        {
            this.connection = transaction.Connection;
            this.From = table;
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
            var join = new JoinQuery(this) { From = table };

            join.Where(new Column(column1), operation, new Column(column2), "AND");

            this.info.Joins.Add(join);
            return this;
        }

        public Query Join(QueryCallback operation)
        {
            var join = new JoinQuery(this);
            operation(join);
            this.info.Joins.Add(join);
            return this;
        }

        #endregion

        public DbDataReader ExecuteReader()
        {
            using (var cmd = this.Grammar.CreateSelect(this))
                return cmd.ExecuteReader();
        }

        public bool Update(params Cell[] cells)
        {
            using (var cmd = this.Grammar.CreateUpdate(this, cells))
                return cmd.ExecuteNonQuery() > 0;
        }

        public void Insert(params Cell[] cells)
        {
            using (var cmd = this.Grammar.CreateUpdate(this, cells))
                cmd.ExecuteNonQuery();
        }

        public bool Delete()
        {
            using (var cmd = this.Grammar.CreateDelete(this))
                return cmd.ExecuteNonQuery() > 0;
        }
    }
}
