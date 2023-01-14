using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryInfo
    {
        public StringBuilder Wheres { get; } = new StringBuilder();
        public List<object> WhereObjs { get; } = new List<object>();

        public Column[] GroupsBy { get; set; } = new Column[0];
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        public ColumnOrder[] Orders { get; set; } = new ColumnOrder[0];
        public Column[] Select { get; set; } = new Column[] { Column.All };

        public IQueryConfig Config { get; }

        public string From { get; set; }
        public string Alias { get; set; }

        public QueryInfo(IQueryConfig config)
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        internal void LoadFrom(QueryInfo info)
        {
            this.Wheres.Clear();
            this.Joins.Clear();

            this.WhereObjs.Clear();

            this.Wheres.Append(info.Wheres);
            this.GroupsBy = (Column[])info.GroupsBy.Clone();
            this.Joins.AddRange(info.Joins);
            this.Orders = (ColumnOrder[])info.Orders.Clone();
            this.Select = (Column[])info.Select.Clone();

            this.WhereObjs.AddRange(info.WhereObjs);
        }
    }
}
