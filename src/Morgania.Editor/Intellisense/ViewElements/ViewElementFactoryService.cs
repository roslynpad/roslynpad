#nullable enable

namespace Microsoft.VisualStudio.Text.Adornments.Implementation;

using System.Composition;

using Avalonia.Controls;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Converts platform-independent content models (classified text, containers, images) into
/// Avalonia controls through ordered <see cref="IViewElementFactory"/> exports, each declaring
/// its (from, to) conversion pair via [TypeConversion]. An extender supersedes a default
/// factory by ordering before it.
/// </summary>
[Export(typeof(IViewElementFactoryService))]
[Shared]
public sealed class ViewElementFactoryService : IViewElementFactoryService
{
    private readonly List<(Lazy<IViewElementFactory, ViewElementFactoryMetadata> Factory, Type From, Type To)> _factories = [];

    [ImportingConstructor]
    public ViewElementFactoryService(
        [ImportMany] IEnumerable<Lazy<IViewElementFactory, ViewElementFactoryMetadata>> factories)
    {
        foreach (var factory in Orderer.Order(factories.ToList()))
        {
            // The conversion pair travels as assembly-qualified names; exports whose types
            // don't resolve in this host can never match and are skipped.
            if (factory.Metadata.FromFullName is { } fromName && factory.Metadata.ToFullName is { } toName
                && Type.GetType(fromName, throwOnError: false) is { } from
                && Type.GetType(toName, throwOnError: false) is { } to)
            {
                _factories.Add((factory, from, to));
            }
        }
    }

    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(model);

        // A control is already a view element (the platform passthrough the contract
        // documents for the native UI element type).
        if (model is Control && model is TView direct)
        {
            return direct;
        }

        foreach (var (factory, from, to) in _factories)
        {
            if (from.IsAssignableFrom(model.GetType()) && typeof(TView).IsAssignableFrom(to))
            {
                return factory.Value.CreateViewElement<TView>(textView, model);
            }
        }

        return null;
    }
}

/// <summary>
/// Concrete metadata view for <see cref="IViewElementFactory"/> exports:
/// [Name]/[Order] plus the [TypeConversion] pair.
/// </summary>
public sealed class ViewElementFactoryMetadata : IOrderable
{
    public ViewElementFactoryMetadata(IDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Name = MetadataValue.Get<string>(data, nameof(Name));
        Before = MetadataValue.GetMany<string>(data, nameof(Before));
        After = MetadataValue.GetMany<string>(data, nameof(After));
        FromFullName = MetadataValue.Get<string>(data, nameof(FromFullName));
        ToFullName = MetadataValue.Get<string>(data, nameof(ToFullName));
    }

    public string Name { get; }

    public IEnumerable<string> Before { get; }

    public IEnumerable<string> After { get; }

    public string FromFullName { get; }

    public string ToFullName { get; }
}
