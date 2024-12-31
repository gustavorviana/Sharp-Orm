using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Linq;

namespace SharpOrm.SqlMethods.Mappers.Sqlite
{
    internal class SqliteStringMethods : SqlMemberCaller<string>
    {
        protected override bool CanWorkMember(SqlMemberInfo member)
        {
            if (member.MemberType == System.Reflection.MemberTypes.Property)
                return member.Name == nameof(string.Length);

            return new[]
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

        protected override SqlExpression GetSqlMethodExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
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
        protected override SqlExpression GetSqlPropertyExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member)
        {
            if (member.Name == nameof(string.Length))
                return new SqlExpression("LENGTH(?)", column);

            throw new NotSupportedException();
        }
    }
}
