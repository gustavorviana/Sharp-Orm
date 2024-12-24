using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.SqlMethods.Mappers.SqlServer
{
    internal class SqlServerStringMethods : SqlMethodCaller
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
                case nameof(string.Trim): return new SqlExpression("LTRIM(RTRIM(?))", expression);
                case nameof(string.TrimStart): return new SqlExpression("LTRIM(?)", expression);
                case nameof(string.TrimEnd): return new SqlExpression("RTRIM(?)", expression);
                case nameof(string.Substring): return SqlMethodMapperUtils.GetSubstringExpression("SUBSTRING", info, expression, method);
                case nameof(string.ToLower): return new SqlExpression("LOWER(?)", expression);
                case nameof(string.ToUpper): return new SqlExpression("UPPER(?)", expression);
                case nameof(string.Concat): return SqlMethodMapperUtils.GetConcatExpression(info, expression, method);
                default: throw new NotSupportedException();
            }
        }
    }
}
