using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm.SqlMethods.Mappers.SqlServer
{
    internal class SqlServerDateProperties : SqlPropertyCaller
    {
        public override bool CanWork(SqlMemberInfo member)
        {
            return TranslationUtils.IsDateOrTime(member.DeclaringType) && new[]
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
                nameof(DateTime.Date),
            }.ContainsIgnoreCase(member.Name);
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member)
        {
            switch (member.Name)
            {
                case nameof(DateTime.UtcNow): return new SqlExpression("GETUTCDATE()");
                case nameof(DateTime.Today): return new SqlExpression("CAST(GETDATE() AS Date)");
                case nameof(DateTime.Now): return new SqlExpression("GETDATE()");
                case nameof(DateTime.DayOfYear): return new SqlExpression("DATEPART(DAYOFYEAR,?)", column);
                case nameof(DateTime.DayOfWeek): return new SqlExpression("DATEPART(WEEKDAY,?)", column);
                case nameof(DateTime.Day): return new SqlExpression("DAY(?)", column);
                case nameof(DateTime.Month): return new SqlExpression("MONTH(?)", column);
                case nameof(DateTime.Year): return new SqlExpression("YEAR(?)", column);
                case nameof(DateTime.Hour): return new SqlExpression("DATEPART(HOUR,?)", column);
                case nameof(DateTime.Minute): return new SqlExpression("DATEPART(MINUTE,?)", column);
                case nameof(DateTime.Second): return new SqlExpression("DATEPART(SECOND,?)", column);
                case nameof(DateTime.Millisecond): return new SqlExpression("DATEPART(MILLISECOND,?)", column);
                case nameof(DateTime.TimeOfDay): return new SqlExpression("CAST(? AS TIME)", column);
                case nameof(DateTime.Date): return new SqlExpression("CAST(? AS DATE)", column);
                default: throw new NotSupportedException();
            }
        }
    }
}
