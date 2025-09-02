using System;

namespace SharpOrm
{
    internal static class DotnetUtils
    {
        public static T[] EmptyArray<T>()
        {
#if NET46_OR_GREATER || NET5_0_OR_GREATER
            return Array.Empty<T>();
#else
            return new T[0];
#endif
        }


        internal static bool SequenceEqual<T>(T[] left, T[] right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (!ReferenceEquals(left[i], right[i]) && !left[i].Equals(right[i]))
                    return false;
            }

            return true;
        }
    }
}
