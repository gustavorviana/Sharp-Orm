using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.SqlMethods.Mappers.Sqlite
{
    internal class SqliteStringMethods : SqlMethodCaller
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
                    nameof(string.ToLower),
                    nameof(string.Concat),
                    nameof(string.ToString)
                }.Contains(member.Name);
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            switch (method.Name)
            {
                case nameof(string.Trim): return new SqlExpression("TRIM(?)", expression);
                case nameof(string.TrimStart): return new SqlExpression("LTRIM(?)", expression);
                case nameof(string.TrimEnd): return new SqlExpression("RTRIM(?)", expression);
                case nameof(string.Substring): return SqlMethodMapperUtils.GetSubstringExpression("SUBSTR", info, expression, method);
                case nameof(string.ToLower): return new SqlExpression("LOWER(?)", expression);
                case nameof(string.ToUpper): return new SqlExpression("UPPER(?)", expression);
                case nameof(string.Concat): return SqlMethodMapperUtils.GetConcatExpression(info, expression, method);
                case nameof(string.ToString): return expression;
                default: throw new NotSupportedException();
            }
        }
    }
}
