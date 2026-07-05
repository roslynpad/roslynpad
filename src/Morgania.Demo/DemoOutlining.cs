namespace Microsoft.VisualStudio.Demo;

using System.Composition;

using Avalonia.Input;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// M5 acceptance: outlining regions over brace blocks, using only contract APIs. Every
/// multi-line <c>{ … }</c> block is collapsible; the editor's elision projection hides
/// the collapsed text.
/// </summary>
[Export(typeof(ITaggerProvider))]
[ContentType("code")]
[TagType(typeof(IOutliningRegionTag))]
public sealed class BraceOutliningTaggerProvider : ITaggerProvider
{
    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(() => new BraceOutliningTagger()) as ITagger<T>;
    }

    private sealed class BraceOutliningTagger : ITagger<IOutliningRegionTag>
    {
#pragma warning disable CS0067 // Regions derive from the snapshot on every query.
        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;
#pragma warning restore CS0067

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            var openStack = new Stack<int>();
            for (int i = 0; i < snapshot.Length; i++)
            {
                char current = snapshot[i];
                if (current == '{')
                {
                    openStack.Push(i);
                }
                else if (current == '}' && openStack.Count > 0)
                {
                    int open = openStack.Pop();
                    var region = new SnapshotSpan(snapshot, open, i + 1 - open);
                    if (snapshot.GetLineNumberFromPosition(region.End) > snapshot.GetLineNumberFromPosition(region.Start)
                        && spans.IntersectsWith(new NormalizedSnapshotSpanCollection(region)))
                    {
                        yield return new TagSpan<IOutliningRegionTag>(
                            region,
                            new OutliningRegionTag(false, false, "{ ... }", region.GetText()));
                    }
                }
            }
        }
    }
}

/// <summary>
/// Binds the demo's outlining gesture: Ctrl(Cmd)+M toggles the innermost collapsible
/// region containing the caret (collapse hides the text through the elision buffer;
/// a second press expands it).
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("code")]
[TextViewRole(PredefinedTextViewRoles.Structured)]
public sealed class OutliningKeyBindings : IWpfTextViewCreationListener
{
    private readonly IOutliningManagerService _outliningManagerService;

    [ImportingConstructor]
    public OutliningKeyBindings(IOutliningManagerService outliningManagerService)
    {
        _outliningManagerService = outliningManagerService;
    }

    public void TextViewCreated(IWpfTextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        if (_outliningManagerService.GetOutliningManager(textView) is not { } manager)
        {
            return;
        }

        textView.VisualElement.KeyDown += (_, e) =>
        {
            if (e.Key == Key.M
                && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
                && !textView.IsClosed)
            {
                ToggleRegionAtCaret(textView, manager);
                e.Handled = true;
            }
        };
    }

    private static void ToggleRegionAtCaret(IWpfTextView textView, IOutliningManager manager)
    {
        var position = textView.Caret.Position.BufferPosition;
        var snapshot = position.Snapshot;
        var probe = new SnapshotSpan(snapshot, position.Position, position.Position < snapshot.Length ? 1 : 0);

        ICollapsible? innermost = null;
        int innermostStart = -1;
        foreach (var region in manager.GetAllRegions(probe))
        {
            var extent = region.Extent.GetSpan(snapshot);
            if ((extent.Contains(position) || extent.End == position) && extent.Start.Position > innermostStart)
            {
                innermost = region;
                innermostStart = extent.Start.Position;
            }
        }

        if (innermost is ICollapsed collapsed)
        {
            manager.Expand(collapsed);
        }
        else if (innermost is not null)
        {
            manager.TryCollapse(innermost);
        }
    }
}
