using System;
using System.Composition;
using RoslynPad.Annotations;

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
        public static T GetService<T>([NotNull] this IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            return (T)serviceProvider.GetService(typeof(T));
        }
    }
}