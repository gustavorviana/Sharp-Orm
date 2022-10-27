using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryInfo
    {
        public StringBuilder Wheres { get; } = new StringBuilder();
        public List<object> WhereObjs { get; } = new List<object>();

        public List<Column> GroupsBy { get; } = new List<Column>();
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        public List<ColumnOrder> Orders { get; } = new List<ColumnOrder>();
        public List<Column> Select { get; } = new List<Column>(new Column[] { Column.All });

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
            this.GroupsBy.Clear();
            this.Joins.Clear();
            this.Orders.Clear();
            this.Select.Clear();

            this.WhereObjs.Clear();

            this.Wheres.Append(info.Wheres);
            this.GroupsBy.AddRange(info.GroupsBy);
            this.Joins.AddRange(info.Joins);
            this.Orders.AddRange(info.Orders);
            this.Select.AddRange(info.Select);

            this.WhereObjs.AddRange(info.WhereObjs);
        }
    }
}
