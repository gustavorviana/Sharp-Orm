using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm.Interceptors
{
    /// <summary>
    /// Interface for implementing save interceptors.
    /// </summary>
    public interface ISaveInterceptor
    {
        Task OnInterceptAsync(ModelInterceptorContext context, CancellationToken cancellation = default);
        void OnIntercept(ModelInterceptorContext context);
    }
}
