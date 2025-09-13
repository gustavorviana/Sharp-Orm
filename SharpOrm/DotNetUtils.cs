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
    }
}
