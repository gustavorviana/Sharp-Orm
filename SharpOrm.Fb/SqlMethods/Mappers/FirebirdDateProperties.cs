using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;

namespace SharpOrm.SqlMethods.Mappers.Firebird
{
    internal class FirebirdDateProperties : SqlPropertyCaller
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
                case nameof(DateTime.Today): return new SqlExpression("CURRENT_DATE");
                case nameof(DateTime.Now): return new SqlExpression("CURRENT_TIMESTAMP");
                case nameof(DateTime.DayOfYear): return new SqlExpression("EXTRACT(YEARDAY FROM ?)", column);
                case nameof(DateTime.DayOfWeek): return new SqlExpression("EXTRACT(WEEKDAY FROM ?)", column);
                case nameof(DateTime.Day): return new SqlExpression("EXTRACT(DAY FROM ?)", column);
                case nameof(DateTime.Month): return new SqlExpression("EXTRACT(MONTH FROM ?)", column);
                case nameof(DateTime.Year): return new SqlExpression("EXTRACT(YEAR FROM ?)", column);
                case nameof(TimeSpan.Hours):
                case nameof(DateTime.Hour): return new SqlExpression("EXTRACT(HOUR FROM ?)", column);
                case nameof(TimeSpan.Minutes):
                case nameof(DateTime.Minute): return new SqlExpression("EXTRACT(MINUTE FROM ?)", column);
                case nameof(TimeSpan.Seconds):
                case nameof(DateTime.Second): return new SqlExpression("EXTRACT(SECOND FROM ?)", column);
                case nameof(TimeSpan.Milliseconds):
                case nameof(DateTime.Millisecond): return new SqlExpression("EXTRACT(MILLISECOND FROM ?)", column);
                case nameof(DateTime.TimeOfDay): return new SqlExpression("CAST(? AS TIME)", column);
                case nameof(DateTime.Date): return new SqlExpression("CAST(? AS DATE)", column);
                case nameof(TimeSpan.MinValue):
                case nameof(TimeSpan.Zero): return new SqlExpression("TIME '00:00:00'");
                case nameof(TimeSpan.MaxValue): return new SqlExpression("TIME '23:59:59.9999'");
                default: throw new NotSupportedException();
            }
        }
    }
}