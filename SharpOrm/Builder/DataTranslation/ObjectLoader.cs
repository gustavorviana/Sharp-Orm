using System;

namespace SharpOrm.Builder.DataTranslation
{
    internal static class ObjectLoader
    {
        public static DateTime ToDatabase(this DateTime date, IQueryConfig config)
        {
            if (config.DateKind == date.Kind)
                return date;

            return config.DateKind == DateTimeKind.Utc ? date.ToUniversalTime() : date.ToLocalTime();
        }

        public static DateTime FromDatabase(this DateTime date, IQueryConfig config)
        {
            if (date.Kind == config.DateKind || config.DateKind == DateTimeKind.Local)
                return date;

            return new DateTime(date.Ticks, DateTimeKind.Utc).ToLocalTime();
        }
    }
}
