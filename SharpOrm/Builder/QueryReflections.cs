using System;

namespace SharpOrm.Builder
{
    internal static class QueryReflections
    {
        public static TableInfo GetTableInfo(this Query query)
        {
            var genericType = query.GetGenericType();
            if (genericType == null)
                return null;

            return query.Config.Translation.GetTable(genericType);
        }

        public static Type GetGenericType(this Query query)
        {
            return IsGenericQuery(query) ? query.GetType().GetGenericArguments()[0] : null;
        }

        public static bool IsGenericQuery(this Query query)
        {
            return query.GetType().IsGenericType &&
                   query.GetType().GetGenericTypeDefinition() == typeof(Query<>);
        }
    }
}
