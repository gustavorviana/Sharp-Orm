using System;
using System.Threading.Tasks;

namespace SharpOrm
{
    internal static class TaskUtils
    {
        public static Task CompletedTask
        {
            get
            {
#if NET45
return Task.FromResult(true);
#else
                return Task.CompletedTask;
#endif
            }
        }

        public static Task<T> Async<T>(Func<T> callback)
        {
            try
            {
                return Task.FromResult(callback());
            }
#pragma warning disable CS0168
            catch (Exception ex)
#pragma warning restore CS0168
            {
#if NET451_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
                return Task<T>.FromException<T>(ex);
#else
                throw;
#endif
            }
        }

        public static Task Async(Action callback)
        {
            try
            {
                return Task.FromResult(callback);
            }
#pragma warning disable CS0168
            catch (Exception ex)
#pragma warning restore CS0168
            {
#if NET451_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
                return Task.FromException(ex);
#else
                throw;
#endif
            }
        }
    }
}
