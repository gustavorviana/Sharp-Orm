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

        internal static StringBuilder AppendJoin<T>(this StringBuilder builder, Action<T> callback, string separator, IEnumerable<T> values)
        {
            using (var en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return builder;

                callback(en.Current);

                while (en.MoveNext())
                {
                    builder.Append(separator);
                    callback(en.Current);
                }

                return builder;
            }
        }

        internal static StringBuilder AppendJoin(this StringBuilder builder, string separator, IEnumerable<object> values)
        {
            using (var en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return builder;

                builder.Append(en.Current);

                while (en.MoveNext())
                    builder.Append(separator).Append(en.Current);

                return builder;
            }
        }
    }
}
