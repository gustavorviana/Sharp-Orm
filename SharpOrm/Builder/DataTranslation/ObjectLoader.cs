using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.DataTranslation
{
    internal static class ObjectLoader
    {
        public static bool CanLoad(object obj, IQueryConfig config)
        {
            return config != null && obj is DateTime;
        }

        public static object LoadFromDatabase(object obj, IQueryConfig config)
        {
            if (obj is DateTime date)
                return date.FromDatabase(config);

            return obj;
        }

        public static object SaveToDatabase(object obj, IQueryConfig config)
        {
            if (obj is DateTime date)
                return date.ToDatabase(config);

            return obj;
        }

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
