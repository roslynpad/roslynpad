#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Connects the outlining manager to the view's elision buffer: collapsing a region
/// elides its extent from the visual buffer, expanding restores it. The region's last
/// character stays visible as the seat for the collapsed-form adornment (which replaces
/// it) — region extents end at a token, never at a line break, so keeping it does not
/// split the joined line. The vendored outlining manager tracks the regions; the view
/// layer owns the projection.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("any")]
[TextViewRole(PredefinedTextViewRoles.Structured)]
public sealed class OutliningElisionSupport : IWpfTextViewCreationListener
{
    private readonly IOutliningManagerService _outliningManagerService;

    [ImportingConstructor]
    public OutliningElisionSupport(IOutliningManagerService outliningManagerService)
    {
        _outliningManagerService = outliningManagerService;
    }

    public void TextViewCreated(IWpfTextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        if (textView.TextViewModel.VisualBuffer is not IElisionBuffer elisionBuffer
            || elisionBuffer.SourceBuffer != textView.TextBuffer
            || _outliningManagerService.GetOutliningManager(textView) is not { } outliningManager)
        {
            return;
        }

        outliningManager.RegionsCollapsed += (_, e) =>
        {
            if (BuildSpans(e.CollapsedRegions.Select(static region => region.Extent), elisionBuffer) is { } spans)
            {
                elisionBuffer.ElideSpans(spans);
            }
        };
        outliningManager.RegionsExpanded += (_, e) =>
        {
            if (BuildSpans(e.ExpandedRegions.Select(static region => region.Extent), elisionBuffer) is { } spans)
            {
                elisionBuffer.ExpandSpans(spans);

                // Expanding a region reveals its whole extent; regions nested inside it
                // that are still collapsed must stay hidden.
                var sourceSnapshot = elisionBuffer.SourceBuffer.CurrentSnapshot;
                var stillCollapsed = e.ExpandedRegions
                    .SelectMany(region => outliningManager.GetCollapsedRegions(region.Extent.GetSpan(sourceSnapshot)))
                    .Select(static region => region.Extent);
                if (BuildSpans(stillCollapsed, elisionBuffer) is { } nestedSpans)
                {
                    elisionBuffer.ElideSpans(nestedSpans);
                }
            }
        };
    }

    private static NormalizedSpanCollection? BuildSpans(IEnumerable<ITrackingSpan> extents, IElisionBuffer elisionBuffer)
    {
        var sourceSnapshot = elisionBuffer.SourceBuffer.CurrentSnapshot;
        List<Span>? spans = null;
        foreach (var extent in extents)
        {
            var span = extent.GetSpan(sourceSnapshot);
            if (span.Length > 1)
            {
                // All but the last character: the visible remainder carries the
                // collapsed-form adornment.
                (spans ??= []).Add(new Span(span.Start, span.Length - 1));
            }
        }

        return spans is null ? null : new NormalizedSpanCollection(spans);
    }
}
