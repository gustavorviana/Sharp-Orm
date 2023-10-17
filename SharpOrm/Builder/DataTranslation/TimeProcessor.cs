using System;

namespace SharpOrm.Builder.DataTranslation
{
    internal static class TimeProcessor
    {
        public static DateTime ToDatabase(this DateTime date, IQueryConfig config)
        {
            if (config.DateKind == date.Kind)
                return date;

            return config.DateKind == DateTimeKind.Utc ? date.ToUniversalTime() : date;
        }

        public static DateTime FromDatabase(this DateTime date, IQueryConfig config)
        {
            if (date.Kind == config.DateKind || config.DateKind == DateTimeKind.Local || config.LocalTimeZone == TimeZoneInfo.Utc)
                return date;

            return TimeZoneInfo.ConvertTimeFromUtc(new DateTime(date.Ticks, DateTimeKind.Utc), config.LocalTimeZone);
        }
    }
}