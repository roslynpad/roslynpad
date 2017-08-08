using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.UI
{
    public interface IAppDispatcher
    {
        void InvokeAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default(CancellationToken));

        Task InvokeTaskAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default(CancellationToken));
    }

    public enum AppDispatcherPriority
    {
        Normal,
        High,
        Low
    }
}