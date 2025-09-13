using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.SqlMethods.Mappers.Firebird
{
    public class FirebirdDateMethods : SqlMethodCaller
    {
        private readonly Dictionary<string, string> dateFormat = new Dictionary<string, string>
        {
            { "yyyy", "EXTRACT(YEAR FROM ?)" },
            { "yy", "SUBSTRING(EXTRACT(YEAR FROM ?) FROM 3 FOR 2)" },
            { "MM", "LPAD(EXTRACT(MONTH FROM ?), 2, '0')" },
            { "M", "EXTRACT(MONTH FROM ?)" },
            { "dd", "LPAD(EXTRACT(DAY FROM ?), 2, '0')" },
            { "d", "EXTRACT(DAY FROM ?)" },
            { "HH", "LPAD(EXTRACT(HOUR FROM ?), 2, '0')" },
            { "H", "EXTRACT(HOUR FROM ?)" },
            { "mm", "LPAD(EXTRACT(MINUTE FROM ?), 2, '0')" },
            { "ss", "LPAD(EXTRACT(SECOND FROM ?), 2, '0')" }
        };

        public override bool CanWork(SqlMemberInfo member)
        {
            return TranslationUtils.IsDateOrTime(member.DeclaringType) &&
                   new[] { nameof(DateTime.ToString) }
                   .ContainsIgnoreCase(member.Name);
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            switch (method.Name)
            {
                case nameof(DateTime.ToString):
                    return new SqlExpression(GetFirebirdFormat(method.Args.FirstOrDefault()?.ToString()
                        ?? SqlMethodMapperUtils.GetDefaultDateOrTimeFormat(method), expression));
                default:
                    throw new NotSupportedException();
            }
        }

        private string GetFirebirdFormat(string format, SqlExpression expr)
        {
            string sql = format;

            foreach (var kv in dateFormat.OrderByDescending(x => x.Key.Length))
            {
                if (sql.Contains(kv.Key))
                    sql = sql.Replace(kv.Key, kv.Value.Replace("?", expr.ToString()));
            }

            return sql;
        }
    }
}
