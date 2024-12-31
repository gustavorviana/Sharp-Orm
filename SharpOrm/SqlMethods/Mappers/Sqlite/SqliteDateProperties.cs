using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;

namespace SharpOrm.SqlMethods.Mappers.Sqlite
{
    internal class SqliteDateProperties : SqlPropertyCaller
    {
        public override bool CanWork(SqlMemberInfo member)
        {
            return TranslationUtils.IsDateOrTime(member.DeclaringType) && new[]
            {
                //DateTime
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
                nameof(DateTime.Date),
                //TimeSpan
                nameof(TimeSpan.Zero),
                nameof(TimeSpan.MaxValue),
                nameof(TimeSpan.MinValue),
                nameof(TimeSpan.Hours),
                nameof(TimeSpan.Minutes),
                nameof(TimeSpan.Seconds),
                nameof(TimeSpan.Milliseconds),
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
                case nameof(TimeSpan.Hours):
                case nameof(DateTime.Hour): return new SqlExpression("STRFTIME('%H',?)", column);
                case nameof(TimeSpan.Minutes):
                case nameof(DateTime.Minute): return new SqlExpression("STRFTIME('%M',?)", column);
                case nameof(TimeSpan.Seconds):
                case nameof(DateTime.Second): return new SqlExpression("STRFTIME('%S',?)", column);
                case nameof(TimeSpan.Milliseconds):
                case nameof(DateTime.Millisecond): return new SqlExpression("(STRFTIME('%f',?)-floor(STRFTIME('%f',?)))*1000", column, column);
                case nameof(DateTime.TimeOfDay): return new SqlExpression("STRFTIME('%H:%M:%S',?)", column);
                case nameof(DateTime.Date): return new SqlExpression("STRFTIME('%Y-%m-%d',?)", column);
                case nameof(TimeSpan.MinValue):
                case nameof(TimeSpan.Zero): return new SqlExpression("CAST('00:00:00' AS TIME)");
                case nameof(TimeSpan.MaxValue): return new SqlExpression("CAST('23:59:59.9999999' AS TIME)");
                default: throw new NotSupportedException();
            }
        }
    }
}
