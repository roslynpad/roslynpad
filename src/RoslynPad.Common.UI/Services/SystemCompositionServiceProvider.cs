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
}
