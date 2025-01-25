using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    internal class ColumnSqlExpression : SqlExpression
    {
        private readonly IReadonlyQueryInfo queryInfo;
        private readonly string columnName;
        private readonly bool needPrefix;

        public ColumnSqlExpression(IReadonlyQueryInfo queryInfo, string columnName, bool needPrefix)
        {
            this.Parameters = new object[0];
            this.queryInfo = queryInfo;
            this.columnName = columnName;
            this.needPrefix = needPrefix;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (needPrefix || queryInfo.NeedPrefix())
                builder.Append(queryInfo.GetColumnPrefix()).Append('.');

            return builder.Append(queryInfo.Config.ApplyNomenclature(columnName)).ToString();
        }
    }
}
