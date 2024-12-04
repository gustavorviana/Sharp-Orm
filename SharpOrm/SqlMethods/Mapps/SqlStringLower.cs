using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps
{
    public class SqlStringLower : SqlMethodCaller
    {
        public override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values)
        {
            return new QueryBuilder(info)
                .Add("LOWER(")
                .Add(expression)
                .Add(')')
                .ToExpression();
        }
    }
}
