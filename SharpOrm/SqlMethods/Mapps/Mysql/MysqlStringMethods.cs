using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Mysql
{
    internal class MysqlStringMethods : SqlMethodCaller<string>
    {
        public override bool CanWork(SqlMemberInfo member)
        {
            return member.DeclaringType == typeof(string) &&
                new[]
                {
                    nameof(string.Substring),
                    nameof(string.Trim),
                    nameof(string.TrimEnd),
                    nameof(string.TrimStart),
                    nameof(string.ToUpper),
                    nameof(string.ToLower)
                }.Contains(member.Name);
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            switch (method.Name)
            {
                case nameof(string.Trim): return new SqlExpression("TRIM(?)", expression);
                case nameof(string.TrimStart): return new SqlExpression("LTRIM(?)", expression);
                case nameof(string.TrimEnd): return new SqlExpression("RTRIM(?)", expression);
                case nameof(string.Substring): return new SqlExpression("SUBSTRING(?,?,?)", expression, method.Args[0], method.Args[1]);
                case nameof(string.ToLower): return new SqlExpression("LOWER(?)", expression);
                case nameof(string.ToUpper): return new SqlExpression("UPPER(?)", expression);
                default: throw new NotSupportedException();
            }
        }
    }
}
