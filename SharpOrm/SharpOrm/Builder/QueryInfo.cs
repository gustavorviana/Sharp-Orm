using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryInfo
    {
        public DbCommand Command { get; }
        public StringBuilder Wheres { get; } = new StringBuilder();
        public StringBuilder GroupsBy { get; } = new StringBuilder();
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        public List<ColumnOrder> Orders { get; } = new List<ColumnOrder>();
        public List<Column> Select { get; } = new List<Column>(new Column[] { Column.All });

        public string From { get; set; }
        public string Alias { get; set; }

        public QueryInfo(DbCommand command)
        {
            this.Command = command;
        }

        public DbCommand SetCommandText(StringBuilder builder)
        {
            this.Command.CommandText = builder.ToString();
            return this.Command;
        }
    }
}
