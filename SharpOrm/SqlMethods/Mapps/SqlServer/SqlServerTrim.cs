using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.SqlServer
{
    internal class SqlServerTrim : SqlMethodCaller
    {
        private readonly TrimMode trimMode;

        public SqlServerTrim(TrimMode trimMode)
        {
            this.trimMode = trimMode;
        }

        public override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values)
        {
            var builder = new QueryBuilder(info);

            if (trimMode == TrimMode.Left || trimMode== TrimMode.All)
                builder.Add("LTRIM(");

            if (trimMode== TrimMode.Right || trimMode== TrimMode.All)
                builder.Add("RTRIM(");

            builder.Add(expression);

            if (trimMode== TrimMode.Left || trimMode== TrimMode.All)
                builder.Add(')');

            if (trimMode== TrimMode.Right || trimMode== TrimMode.All)
                builder.Add(')');

            return builder.ToExpression();
        }
    }
}
