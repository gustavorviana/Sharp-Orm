using System;
using System.Threading.Tasks;

namespace SharpOrm
{
    /// <summary>
    /// Utility class for safely disposing objects and executing actions without propagating exceptions.
    /// </summary>
    public static class DisposeUtils
    {
        /// <summary>
        /// Action to log exceptions during safe dispose operations.
        /// Set this to enable exception logging during dispose operations.
        /// </summary>
        public static Action<string, Exception> ExceptionLogger { get; set; }

        /// <summary>
        /// Safely disposes an object, catching and optionally logging any exceptions.
        /// </summary>
        /// <param name="disposable">The object to dispose.</param>
        /// <param name="context">Optional context description for logging purposes.</param>
        public static void SafeDispose(IDisposable disposable, string context = null)
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                ExceptionLogger?.Invoke(context ?? $"Disposing {disposable.GetType().Name}", ex);
            }
        }

        /// <summary>
        /// Safely executes an action, catching and optionally logging any exceptions.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="context">Optional context description for logging purposes.</param>
        public static void SafeExecute(Action action, string context = null)
        {
            if (action == null)
                return;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                ExceptionLogger?.Invoke(context ?? "Executing action", ex);
            }
        }

        /// <summary>
        /// Safely executes an async action, catching and optionally logging any exceptions.
        /// </summary>
        /// <param name="asyncAction">The async action to execute.</param>
        /// <param name="context">Optional context description for logging purposes.</param>
        public static async Task SafeExecuteAsync(Func<Task> asyncAction, string context = null)
        {
            if (asyncAction == null)
                return;

            try
            {
                await asyncAction();
            }
            catch (Exception ex)
            {
                ExceptionLogger?.Invoke(context ?? "Executing async action", ex);
            }
        }
    }
}

