using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryInfo
    {
        public StringBuilder Wheres { get; } = new StringBuilder();
        public List<object> WhereObjs { get; } = new List<object>();

        public StringBuilder GroupsBy { get; } = new StringBuilder();
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        public List<ColumnOrder> Orders { get; } = new List<ColumnOrder>();
        public List<Column> Select { get; } = new List<Column>(new Column[] { Column.All });

        public string From { get; set; }
        public string Alias { get; set; }

        internal void LoadFrom(QueryInfo info)
        {
            this.Wheres.Append(info.Wheres);
            this.GroupsBy.Append(info.GroupsBy);
            this.Joins.AddRange(info.Joins);
            this.Orders.AddRange(info.Orders);
            this.Select.AddRange(info.Select);

            this.WhereObjs.AddRange(info.WhereObjs);
        }
    }
}
