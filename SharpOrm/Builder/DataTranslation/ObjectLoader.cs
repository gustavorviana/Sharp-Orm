using System;

namespace SharpOrm.Builder.DataTranslation
{
    internal static class ObjectLoader
    {
        public static DateTime ToDatabase(this DateTime date, IQueryConfig config)
        {
            if (config.DateKind == date.Kind || config.DateKind == DateTimeKind.Unspecified)
                return date;

            if (config.DateKind == DateTimeKind.Utc)
                return date.ToUniversalTime();

            return date.ToLocalTime();
        }

        public static DateTime FromDatabase(this DateTime date, IQueryConfig config)
        {
            if (config.DateKind == DateTimeKind.Unspecified || config.DateKind == DateTimeKind.Local)
                return date;

            return new DateTime(date.Ticks, DateTimeKind.Utc).ToLocalTime();
        }
    }
}
