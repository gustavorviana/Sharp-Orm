using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm.Interceptors
{
    public abstract class SaveInterceptor : ISaveInterceptor
    {
        public virtual async Task OnInterceptAsync(ModelInterceptorContext context, CancellationToken cancellation = default)
        {
            OnIntercept(context);
            await TaskUtils.CompletedTask;
        }

        public virtual void OnIntercept(ModelInterceptorContext context)
        {

        }
    }
}
