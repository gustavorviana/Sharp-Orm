using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    internal static class BuilderExt
    {
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
