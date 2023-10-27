using System;
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
    }
}
