using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Mysql
{
    internal class MySqlSubstring : SqlMethodCaller
    {
        public override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values)
        {
            return new QueryBuilder(info)
                .Add("SUBSTRING(")
                .Add(expression)
                .Add(new SqlExpression(",?,?", values))
                .Add(')')
                .ToExpression();
        }
    }
}
