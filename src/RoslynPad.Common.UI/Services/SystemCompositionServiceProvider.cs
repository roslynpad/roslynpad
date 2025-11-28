using System.Composition;

namespace RoslynPad.UI;

[Export(typeof(IServiceProvider)), Shared]
[method: ImportingConstructor]
internal class SystemCompositionServiceProvider(CompositionContext host) : IServiceProvider
{
    private readonly CompositionContext _host = host;

    public object GetService(Type serviceType)
    {
        return _host.GetExport(serviceType);
    }
}
