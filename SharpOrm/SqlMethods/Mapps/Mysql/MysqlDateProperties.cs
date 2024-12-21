using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Mysql
{
    internal class MysqlDateProperties : SqlPropertyCaller<DateTime>
    {
        public override bool CanWork(SqlMemberInfo member)
        {
            return new[]
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
                case nameof(DateTime.UtcNow): return new SqlExpression("UTC_TIMESTAMP()");
                case nameof(DateTime.Today): return new SqlExpression("CURDATE()");
                case nameof(DateTime.Now): return new SqlExpression("NOW()");
                case nameof(DateTime.DayOfYear): return new SqlExpression("DAYOFYEAR(?)", column);
                case nameof(DateTime.DayOfWeek): return new SqlExpression("DAYOFWEEK(?)", column);
                case nameof(DateTime.Day): return new SqlExpression("DAY(?)", column);
                case nameof(DateTime.Month): return new SqlExpression("MONTH(?)", column);
                case nameof(DateTime.Year): return new SqlExpression("YEAR(?)", column);
                case nameof(DateTime.Hour): return new SqlExpression("DATE_FORMAT(?,'%H')", column);
                case nameof(DateTime.Minute): return new SqlExpression("DATE_FORMAT(?,'%i')", column);
                case nameof(DateTime.Second): return new SqlExpression("DATE_FORMAT(?,'%s')", column);
                case nameof(DateTime.Millisecond): return new SqlExpression("MICROSECOND(?)/1000", column);
                case nameof(DateTime.TimeOfDay): return new SqlExpression("TIME(?)", column);
                case nameof(DateTime.Date): return new SqlExpression("DATE(?)", column);
                default: throw new NotSupportedException();
            }
        }
    }
}
