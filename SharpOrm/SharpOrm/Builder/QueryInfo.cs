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

        internal void LoadFrom(QueryInfo info)
        {
            this.Wheres.Append(info.Wheres);
            this.GroupsBy.Append(info.GroupsBy);
            this.Joins.AddRange(info.Joins);
            this.Orders.AddRange(info.Orders);
            this.Select.AddRange(info.Select);

            foreach (DbParameter param in info.Command.Parameters)
                info.CopyParam(this.Command, param);
        }

        private void CopyParam(DbCommand target, DbParameter parameter)
        {
            var newParam = target.CreateParameter();

            newParam.DbType = parameter.DbType;
            newParam.Direction = parameter.Direction;
            newParam.IsNullable = parameter.IsNullable;
            newParam.ParameterName = parameter.ParameterName;
            newParam.Precision = parameter.Precision;
            newParam.Scale = parameter.Scale;
            newParam.Size = parameter.Size;
            newParam.SourceColumn = parameter.SourceColumn;
            newParam.SourceColumnNullMapping = parameter.SourceColumnNullMapping;
            newParam.SourceVersion = parameter.SourceVersion;
            newParam.Value = parameter.Value;

            target.Parameters.Add(parameter);
        }
    }
}
