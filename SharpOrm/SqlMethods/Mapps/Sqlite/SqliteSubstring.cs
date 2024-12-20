using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Sqlite
{
    internal class SqliteSubstring : SqlMethodCaller
    {
        public override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values)
        {
            return new QueryBuilder(info)
                .Add("SUBSTR(")
                .Add(expression)
                .Add(new SqlExpression(",?,?", values))
                .Add(')')
                .ToExpression();
        }
    }
}
