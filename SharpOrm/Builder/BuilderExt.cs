﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    internal static class BuilderExt
    {
        internal static int Count(this StringBuilder builder, int charToCount)
        {
            int qtd = 0;
            for (int i = 0; i < builder.Length; i++)
                if (builder[i] == charToCount)
                    qtd++;

            return qtd;
        }

        internal static string Replace(this string str, char toReplace, Func<int, string> func)
        {
            return new StringBuilder().AppendReplaced(str, toReplace, func).ToString();
        }

        internal static StringBuilder AppendReplaced(this StringBuilder builder, string toAdd, char toReplace, Func<int, string> func)
        {
            return AppendAndReplace(builder, toAdd, toReplace, index => builder.Append(func(index)));
        }

        internal static StringBuilder AppendAndReplace(this StringBuilder builder, string toAdd, char toReplace, Action<int> call)
        {
            builder.Capacity += toAdd.Length;

            int count = 0;
            for (int i = 0; i < toAdd.Length; i++)
            {
                char c = toAdd[i];
                if (c != toReplace)
                {
                    builder.Append(c);
                    continue;
                }

                count++;
                call(count);
            }

            return builder;
        }

        internal static StringBuilder AppendJoin<T>(this StringBuilder builder, Action<T> callback, string separator, IEnumerator<T> en)
        {
            callback(en.Current);

            while (en.MoveNext())
            {
                builder.Append(separator);
                callback(en.Current);
            }

            return builder;
        }

        internal static StringBuilder AppendJoin<T>(this StringBuilder builder, string separator, IEnumerable<T> values)
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
