﻿using System;
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
    }
}
