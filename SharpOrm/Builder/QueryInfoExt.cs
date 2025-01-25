using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    internal static class QueryInfoExt
    {
        public static bool NeedPrefix(this IReadonlyQueryInfo info)
        {
            return info is QueryInfo qi && qi.Joins.Count > 0;
        }

        public static string GetColumnPrefix(this IReadonlyQueryInfo info)
        {
            return info.Config.ApplyNomenclature(info.TableName.TryGetAlias(info.Config));
        }

        public static bool IsExpectedType(this IRootTypeMap map, Type type)
        {
            return map.RootType == type || type.IsAssignableFrom(map.RootType);
        }

        public static bool IsExpectedType(this JoinQuery query, Type type)
        {
            var rootType = (query as IRootTypeMap).RootType;
            return rootType == type || rootType.IsAssignableFrom(type);
        }
    }
}
