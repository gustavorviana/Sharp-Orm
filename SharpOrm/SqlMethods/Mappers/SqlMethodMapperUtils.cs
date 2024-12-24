using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;

namespace SharpOrm.SqlMethods.Mappers
{
    public static class SqlMethodMapperUtils
    {
        public static string GetDefaultDateOrTimeFormat(SqlMethodInfo method)
        {
            return GetDefaultDateOrTimeFormat(method.DeclaringType);
        }

        public static SqlExpression GetSubstringExpression(string functionName, IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            if (method.Args.Length == 0 || method.Args.Length > 2)
                throw new NotSupportedException();

            var builder = new QueryBuilder(info);
            builder.Add(functionName).Add("(").AddParameter(expression);
            SqlMethodMapperUtils.WriteArgs(builder, method.Args);

            return builder.Add(')').ToExpression();
        }

        public static SqlExpression GetConcatExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            if (method.Args.Length < 2)
                throw new NotSupportedException();

            var builder = new QueryBuilder(info);
            builder.Add("CONCAT(");
            SqlMethodMapperUtils.WriteArgs(builder, method.Args, 0, false);

            return builder.Add(')').ToExpression();
        }

        public static string GetDefaultDateOrTimeFormat(Type declaringType)
        {
            if (declaringType == typeof(TimeSpan))
                return "HH:mm:ss";

#if NET6_0_OR_GREATER
            if (declaringType == typeof(TimeOnly))
                return "HH:mm:ss";

            if (declaringType == typeof(DateOnly))
                return "yyyy-MM-dd";
#endif

            return "yyyy-MM-dd HH:mm:ss";
        }

        internal static void WriteArgs(QueryBuilder builder, object[] args, int offset = 0, bool firstComma = true)
        {
            if (args.Length == 0)
                return;

            if (firstComma)
                builder.Add(',');

            builder.AddParameter(args[offset]);

            for (int i = offset + 1; i < args.Length; i++)
                builder.Add(',').AddParameter(args[i]);
        }
    }
}
