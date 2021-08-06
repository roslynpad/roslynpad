using System;
using System.Composition;

namespace RoslynPad.UI
{
    [Export(typeof(IServiceProvider)), Shared]
    internal class SystemCompositionServiceProvider : IServiceProvider
    {
        private readonly CompositionContext _host;

        [ImportingConstructor]
        public SystemCompositionServiceProvider(CompositionContext host)
        {
            _host = host;
        }
        
        public object GetService(Type serviceType)
        {
            return _host.GetExport(serviceType);
        }
    }

    internal static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            return serviceProvider.GetService(typeof(T)) as T ?? throw new InvalidOperationException("Unable to find service");
        }
    }
}
