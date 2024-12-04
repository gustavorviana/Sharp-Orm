using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Operators
{
    public interface ISqlMethod
    {
        SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values);
    }
}
