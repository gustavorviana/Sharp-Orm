using System;
using System.Collections.Generic;
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

        internal static StringBuilder AppendReplaced(this StringBuilder builder, string toAdd, char toReplace, Func<int, string> func)
        {
            builder.Capacity += toAdd.Length;

            int count = 0;
            foreach (var c in toAdd)
            {
                if (c != toReplace)
                {
                    builder.Append(c);
                    continue;
                }

                count++;
                builder.Append(func(count));
            }

            return builder;
        }

        internal static IEnumerable<int> GetIndexesOfParamsChar(this StringBuilder builder)
        {
            for (int i = 0; i < builder.Length; i++)
                if (builder[i] == '?')
                    yield return i;
        }
    }
}
