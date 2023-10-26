using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    public class QueryInfo
    {
        private IReadonlyQueryInfo _queryInfo;

        public QueryConstructor Where { get; }

        public Column[] GroupsBy { get; set; } = new Column[0];
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        public ColumnOrder[] Orders { get; set; } = new ColumnOrder[0];
        public Column[] Select { get; set; } = new Column[] { Column.All };

        public IQueryConfig Config { get; }

        public DbName TableName { get; }

        public string From => this.TableName.Name;

        public string Alias => this.TableName.Alias;

        public QueryInfo(IQueryConfig config, DbName table)
        {
            this.TableName = table;
            this._queryInfo = new ReadonlyInfo(this);
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
            this.Where = new QueryConstructor(this.ToReadOnly());
        }

        internal void LoadFrom(QueryInfo info)
        {
            this.Where.Clear();
            this.Joins.Clear();

            this.Where.Add(info.Where);
            this.Joins.AddRange(info.Joins);
            this.GroupsBy = (Column[])info.GroupsBy.Clone();
            this.Orders = (ColumnOrder[])info.Orders.Clone();
            this.Select = (Column[])info.Select.Clone();
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

        private class ReadonlyInfo : IReadonlyQueryInfo
        {
            private readonly QueryInfo info;

            public ReadonlyInfo(QueryInfo info)
            {
                this.info = info;
            }

            public IQueryConfig Config => this.info.Config;

            public DbName TableName => this.info.TableName;
        }
    }
}