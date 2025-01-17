﻿using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;

namespace SharpOrm.SqlMethods.Mappers.SqlServer
{
    internal class SqlServerDateProperties : SqlPropertyCaller
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
                //DateTime
                case nameof(DateTime.UtcNow): return new SqlExpression("GETUTCDATE()");
                case nameof(DateTime.Today): return new SqlExpression("CAST(GETDATE() AS Date)");
                case nameof(DateTime.Now): return new SqlExpression("GETDATE()");
                case nameof(DateTime.DayOfYear): return new SqlExpression("DATEPART(DAYOFYEAR,?)", column);
                case nameof(DateTime.DayOfWeek): return new SqlExpression("DATEPART(WEEKDAY,?)", column);
                case nameof(DateTime.Day): return new SqlExpression("DAY(?)", column);
                case nameof(DateTime.Month): return new SqlExpression("MONTH(?)", column);
                case nameof(DateTime.Year): return new SqlExpression("YEAR(?)", column);
                case nameof(TimeSpan.Hours):
                case nameof(DateTime.Hour): return new SqlExpression("DATEPART(HOUR,?)", column);
                case nameof(TimeSpan.Minutes):
                case nameof(DateTime.Minute): return new SqlExpression("DATEPART(MINUTE,?)", column);
                case nameof(TimeSpan.Seconds):
                case nameof(DateTime.Second): return new SqlExpression("DATEPART(SECOND,?)", column);
                case nameof(TimeSpan.Milliseconds):
                case nameof(DateTime.Millisecond): return new SqlExpression("DATEPART(MILLISECOND,?)", column);
                case nameof(DateTime.TimeOfDay): return new SqlExpression("CAST(? AS TIME)", column);
                case nameof(DateTime.Date): return new SqlExpression("CAST(? AS DATE)", column);
                case nameof(TimeSpan.MinValue):
                case nameof(TimeSpan.Zero): return new SqlExpression("CAST('00:00:00' AS TIME)");
                case nameof(TimeSpan.MaxValue): return new SqlExpression("CAST('23:59:59.9999999' AS TIME)");
                default: throw new NotSupportedException();
            }
        }
    }
}