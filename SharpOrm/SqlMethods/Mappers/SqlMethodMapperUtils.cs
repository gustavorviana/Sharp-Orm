using SharpOrm.Builder.Expressions;
using System;

namespace SharpOrm.SqlMethods.Mappers
{
    public static class SqlMethodMapperUtils
    {
        public static string GetDefaultDateOrTimeFormat(SqlMethodInfo method)
        {
            return GetDefaultDateOrTimeFormat(method.DeclaringType);
        }

        public static string GetDefaultDateOrTimeFormat(Type declaringType)
        {
            if (declaringType == typeof(TimeSpan))
                return "HH:mm:ss";

#if NET6_0_OR_GREATER
            if (declaringType == typeof(TimeOnly))
                return "HH:mm:ss";

            if (declaringType == typeof(DateOnly))
                return "yyyy-MM-dd";
#endif

            return "yyyy-MM-dd HH:mm:ss";
        }
    }
}
