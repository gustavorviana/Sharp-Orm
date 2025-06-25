using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.SqlMethods;
using SharpOrm.SqlMethods.Mappers;
using System;
using System.Linq;

namespace SharpOrm.Fb.SqlMethods.Mappers
{
    internal class FirebirdStringMethods : SqlMemberCaller<string>
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
                case nameof(string.TrimStart): return new SqlExpression("TRIM(LEADING FROM ?)", expression);
                case nameof(string.TrimEnd): return new SqlExpression("TRIM(TRAILING FROM ?)", expression);
                case nameof(string.Substring): return GetSubstringExpression(info, expression, method);
                case nameof(string.ToLower): return new SqlExpression("LOWER(?)", expression);
                case nameof(string.ToUpper): return new SqlExpression("UPPER(?)", expression);
                case nameof(string.Concat): return GetConcatExpression(info, expression, method);
                case nameof(string.ToString): return expression;
                default: throw new NotSupportedException();
            }
        }

        private static SqlExpression GetSubstringExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            if (method.Args.Length == 0 || method.Args.Length > 2)
                throw new NotSupportedException();

            var builder = new QueryBuilder(info)
                .Add("SUBSTRING(")
                .AddParameter(expression)
                .Add(" FROM ")
                .AddParameter(method.Args[0]);

            if (method.Args.Length == 2)
                builder.Add(" FOR ").AddParameter(method.Args[1]);

            return builder.Add(')').ToExpression();
        }

        private static SqlExpression GetConcatExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            if (method.Args.Length < 2)
                throw new NotSupportedException();

            var builder = new QueryBuilder(info);
            SqlMethodMapperUtils.WriteArgs(builder, method.Args, 0, false, "||");

            return builder.ToExpression();
        }

        protected override SqlExpression GetSqlPropertyExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member)
        {
            if (member.Name == nameof(string.Length))
                return new SqlExpression("CHAR_LENGTH(?)", column);
            throw new NotSupportedException();
        }
    }
}