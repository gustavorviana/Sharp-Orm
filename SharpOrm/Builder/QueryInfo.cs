using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    public class QueryInfo
    {
        private IReadonlyQueryInfo _queryInfo;

        public QueryBuilder Where { get; }
        public QueryBuilder Having { get; }

        public Column[] GroupsBy { get; set; } = new Column[0];
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        public ColumnOrder[] Orders { get; set; } = new ColumnOrder[0];
        public Column[] Select { get; set; } = new Column[] { Column.All };

        public QueryConfig Config { get; }

        public DbName TableName { get; }

        public string From => this.TableName.Name;

        public string Alias => this.TableName.Alias;

        public QueryInfo(QueryConfig config, DbName table)
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
            this._queryInfo = new ReadonlyQueryInfo(config, table);
            this.TableName = table;

            this.Where = new QueryBuilder(this.ToReadOnly());
            this.Having = new QueryBuilder(this.ToReadOnly());
        }

        internal void LoadFrom(QueryInfo info)
        {
            this.Joins.Clear();
            this.Where.Clear();
            this.Having.Clear();

            this.Where.Add(info.Where);
            this.Having.Add(info.Having);
            this.Joins.AddRange(info.Joins);
            this.Select = (Column[])info.Select.Clone();
            this.GroupsBy = (Column[])info.GroupsBy.Clone();
            this.Orders = (ColumnOrder[])info.Orders.Clone();
        }

        public IReadonlyQueryInfo ToReadOnly()
        {
            return this._queryInfo;
        }

        internal bool IsCount()
        {
            if (this.Select.Length != 1)
                return false;

            string select = this.Select[0].ToExpression(this.ToReadOnly()).ToString().ToLower();
            return select.StartsWith("count(");
        }
    }
}