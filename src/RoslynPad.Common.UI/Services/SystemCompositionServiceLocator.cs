using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.Practices.ServiceLocation;

namespace RoslynPad.UI
{
    [Export(typeof(IServiceLocator)), Shared]
    internal class SystemCompositionServiceLocator : ServiceLocatorImplBase
    {
        private readonly CompositionContext _host;

        [ImportingConstructor]
        public SystemCompositionServiceLocator(CompositionContext host)
        {
            _host = host;
            Thread.MemoryBarrier();
            ServiceLocator.SetLocatorProvider(() => this);
        }

        protected override object DoGetInstance(Type serviceType, string key)
        {
            return _host.GetExport(serviceType, key);
        }

        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            return _host.GetExports(serviceType);
        }
    }
}