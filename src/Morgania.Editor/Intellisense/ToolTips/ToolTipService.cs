#nullable enable

namespace Microsoft.VisualStudio.Text.Adornments.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The Modern ToolTip service: presenters come from ordered <see cref="IToolTipPresenterFactory"/>
/// exports, so an extender can supersede the default presenter by ordering before it.
/// The vendored async Quick Info broker presents through this service.
/// </summary>
[Export(typeof(IToolTipService))]
[Shared]
public sealed class ToolTipService : IToolTipService
{
    private readonly List<Lazy<IToolTipPresenterFactory, Orderable>> _factories;

    [ImportingConstructor]
    public ToolTipService([ImportMany] IEnumerable<Lazy<IToolTipPresenterFactory, Orderable>> factories)
    {
        _factories = Orderer.Order(factories.ToList()).ToList();
    }

    public IToolTipPresenter CreatePresenter(ITextView textView, ToolTipParameters? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(textView);
        parameters ??= ToolTipParameters.Default;
        foreach (var factory in _factories)
        {
            if (factory.Value.Create(textView, parameters) is { } presenter)
            {
                return presenter;
            }
        }

        throw new InvalidOperationException("No IToolTipPresenterFactory is exported.");
    }
}
