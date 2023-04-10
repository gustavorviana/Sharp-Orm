using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryInfo
    {
        private IReadonlyQueryInfo _queryInfo;

        public StringBuilder Where { get; } = new StringBuilder();
        public List<object> WhereObjs { get; } = new List<object>();

        public Column[] GroupsBy { get; set; } = new Column[0];
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        public ColumnOrder[] Orders { get; set; } = new ColumnOrder[0];
        public Column[] Select { get; set; } = new Column[] { Column.All };

        public IQueryConfig Config { get; }

        public TableName Table { get; set; }

        public string From => this.Table.Name;

        public string Alias => this.Table.Alias;

        public QueryInfo(IQueryConfig config)
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        internal void LoadFrom(QueryInfo info)
        {
            this.Where.Clear();
            this.Joins.Clear();

            this.WhereObjs.Clear();

            this.Where.Append(info.Where);
            this.GroupsBy = (Column[])info.GroupsBy.Clone();
            this.Joins.AddRange(info.Joins);
            this.Orders = (ColumnOrder[])info.Orders.Clone();
            this.Select = (Column[])info.Select.Clone();

            this.WhereObjs.AddRange(info.WhereObjs);
        }

        public IReadonlyQueryInfo ToReadOnly()
        {
            if (this._queryInfo == null)
                this._queryInfo = new ReadonlyInfo(this);

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

            public string From => this.info.From;

            public string Alias => this.info.Alias;
        }
    }
}
