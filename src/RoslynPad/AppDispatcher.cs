using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(IAppDispatcher))]
    public class AppDispatcher : DispatcherObject, IAppDispatcher
    {
        public void InvokeAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal,
            CancellationToken cancellationToken = new CancellationToken())
        {
            InternalInvoke(action, priority, cancellationToken);
        }

        public Task InvokeTaskAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return InternalInvoke(action, priority, cancellationToken).Task;
        }

        private DispatcherOperation InternalInvoke(Action action, AppDispatcherPriority priority, CancellationToken cancellationToken)
        {
            return Dispatcher.InvokeAsync(action, ConvertPriority(priority), cancellationToken);
        }

        private DispatcherPriority ConvertPriority(AppDispatcherPriority priority)
        {
            switch (priority)
            {
                case AppDispatcherPriority.Normal:
                    return DispatcherPriority.Normal;
                case AppDispatcherPriority.High:
                    return DispatcherPriority.Send;
                case AppDispatcherPriority.Low:
                    return DispatcherPriority.Background;
                default:
                    throw new ArgumentOutOfRangeException(nameof(priority), priority, null);
            }
        }
    }
}