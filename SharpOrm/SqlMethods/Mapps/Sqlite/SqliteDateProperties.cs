using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Sqlite
{
    internal class SqliteDateProperties : SqlPropertyCaller<DateTime>
    {
        public override bool CanWork(SqlMemberInfo member)
        {
            return member.DeclaringType == typeof(DateTime) && new[]
            {
                nameof(DateTime.UtcNow),
                nameof(DateTime.Today),
                nameof(DateTime.Now),
                nameof(DateTime.DayOfYear),
                nameof(DateTime.DayOfWeek),
                nameof(DateTime.Day),
                nameof(DateTime.Month),
                nameof(DateTime.Year),
                nameof(DateTime.Hour),
                nameof(DateTime.Minute),
                nameof(DateTime.Second),
                nameof(DateTime.Millisecond),
                nameof(DateTime.TimeOfDay),
                nameof(DateTime.Date)
            }.ContainsIgnoreCase(member.Name);
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member)
        {
            switch (member.Name)
            {
                case nameof(DateTime.UtcNow): return new SqlExpression("CURRENT_TIMESTAMP");
                case nameof(DateTime.Today): return new SqlExpression("DATE('now')");
                case nameof(DateTime.Now): return new SqlExpression("DATETIME()");
                case nameof(DateTime.DayOfYear): return new SqlExpression("STRFTIME('%j',?)", column);
                case nameof(DateTime.DayOfWeek): return new SqlExpression("STRFTIME('%w',?)+1", column);
                case nameof(DateTime.Day): return new SqlExpression("STRFTIME('%d',?)", column);
                case nameof(DateTime.Month): return new SqlExpression("STRFTIME('%m',?)", column);
                case nameof(DateTime.Year): return new SqlExpression("STRFTIME('%Y',?)", column);
                case nameof(DateTime.Hour): return new SqlExpression("STRFTIME('%H',?)", column);
                case nameof(DateTime.Minute): return new SqlExpression("STRFTIME('%M',?)", column);
                case nameof(DateTime.Second): return new SqlExpression("STRFTIME('%S',?)", column);
                case nameof(DateTime.Millisecond): return new SqlExpression("(STRFTIME('%f',?)-floor(STRFTIME('%f',?)))*1000", column, column);
                case nameof(DateTime.TimeOfDay): return new SqlExpression("STRFTIME('%H:%M:%S',?)", column);
                case nameof(DateTime.Date): return new SqlExpression("STRFTIME('%Y-%m-%d',?)", column);
                default: throw new NotSupportedException();
            }
        }
    }
}
