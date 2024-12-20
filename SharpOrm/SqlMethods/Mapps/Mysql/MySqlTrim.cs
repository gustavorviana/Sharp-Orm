using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Mysql
{
    internal class MySqlTrim : SqlMethodCaller
    {
        private readonly TrimMode trimMode;

        public MySqlTrim(TrimMode trimMode)
        {
            this.trimMode = trimMode;
        }

        public override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, object[] values)
        {
            var builder = new QueryBuilder(info);
            switch (trimMode)
            {
                case TrimMode.Left:
                    builder.Add("LTRIM(");
                    break;
                case TrimMode.Right:
                    builder.Add("RTRIM(");
                    break;
                default:
                    builder.Add("TRIM(");
                    break;
            }

            builder.Add(expression);

            return builder.Add(')').ToExpression();
        }
    }
}
