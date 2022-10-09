using System;
using System.Text;

namespace SharpOrm.Builder
{
    public static class BuilderExt
    {
        internal static StringBuilder Replace(this StringBuilder builder, char c, Func<int, string> func)
        {
            int count = 0;
            for (int i = 0; i < builder.Length; i++)
            {
                if (builder[i] != c)
                    continue;

                count++;
                builder.Remove(i, 1).Insert(i, func(count));
            }

            return builder;
        }

        public static QueryInfo GetInfo(this QueryBase query)
        {
            return query.info;
        }
    }
}
