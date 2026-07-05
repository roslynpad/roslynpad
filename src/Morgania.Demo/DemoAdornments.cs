namespace Microsoft.VisualStudio.Demo;

using System.Composition;
using System.Text.RegularExpressions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// M3 acceptance: an intra-text color swatch for #RRGGBB literals, using only contract APIs
/// (an <see cref="IntraTextAdornmentTag"/> view tagger; the editor negotiates the space).
/// </summary>
[Export(typeof(IViewTaggerProvider))]
[ContentType("code")]
[TagType(typeof(IntraTextAdornmentTag))]
public sealed partial class ColorSwatchTaggerProvider : IViewTaggerProvider
{
    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        where T : ITag
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(() => new ColorSwatchTagger()) as ITagger<T>;
    }

    private sealed partial class ColorSwatchTagger : ITagger<IntraTextAdornmentTag>
    {
        private readonly Dictionary<ITrackingSpan, IntraTextAdornmentTag> _cache = new();

#pragma warning disable CS0067 // Static content: the tags never change after load.
        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;
#pragma warning restore CS0067

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                foreach (Match match in ColorLiteral().Matches(span.GetText()))
                {
                    var literalSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    yield return new TagSpan<IntraTextAdornmentTag>(literalSpan, GetOrCreateTag(literalSpan, match.Value));
                }
            }
        }

        private IntraTextAdornmentTag GetOrCreateTag(SnapshotSpan literalSpan, string literal)
        {
            // One stable tag per literal occurrence: the identity must survive relayouts so
            // the support component can track the visual.
            foreach (var (trackingSpan, existingTag) in _cache)
            {
                if (trackingSpan.GetSpan(literalSpan.Snapshot) == literalSpan)
                {
                    return existingTag;
                }
            }

            var swatch = new Border
            {
                Width = 56.0,
                Height = 12.0,
                CornerRadius = new CornerRadius(3.0),
                Background = new SolidColorBrush(Color.Parse(literal)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1.0),
            };
            var tag = new IntraTextAdornmentTag(swatch, removalCallback: null);
            _cache[literalSpan.Snapshot.CreateTrackingSpan(literalSpan, SpanTrackingMode.EdgeExclusive)] = tag;
            return tag;
        }

        [GeneratedRegex("#[0-9a-fA-F]{6}")]
        private static partial Regex ColorLiteral();
    }
}

/// <summary>
/// M3 acceptance: brace-highlight markers on the TextMarker adornment layer, using only
/// contract APIs (creation listener, caret events, marker geometry from the view lines).
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("code")]
public sealed class BraceHighlightListener : IWpfTextViewCreationListener
{
    private static readonly IBrush MarkerBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x77, 0x77, 0x30));

    public void TextViewCreated(IWpfTextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        textView.Caret.PositionChanged += (_, _) => UpdateMarkers(textView);
        textView.LayoutChanged += (_, _) => UpdateMarkers(textView);
    }

    private static void UpdateMarkers(IWpfTextView textView)
    {
        if (textView.IsClosed || textView.InLayout)
        {
            return;
        }

        var layer = textView.GetAdornmentLayer(PredefinedAdornmentLayers.TextMarker);
        layer.RemoveAllAdornments();

        var caret = textView.Caret.Position.BufferPosition;
        var snapshot = textView.TextSnapshot;
        if (caret.Position >= snapshot.Length)
        {
            return;
        }

        char character = caret.GetChar();
        int matchPosition = character switch
        {
            '(' => FindMatch(snapshot, caret.Position, '(', ')', forward: true),
            '{' => FindMatch(snapshot, caret.Position, '{', '}', forward: true),
            '[' => FindMatch(snapshot, caret.Position, '[', ']', forward: true),
            ')' => FindMatch(snapshot, caret.Position, ')', '(', forward: false),
            '}' => FindMatch(snapshot, caret.Position, '}', '{', forward: false),
            ']' => FindMatch(snapshot, caret.Position, ']', '[', forward: false),
            _ => -1,
        };

        if (matchPosition < 0)
        {
            return;
        }

        AddMarker(textView, layer, new SnapshotSpan(snapshot, caret.Position, 1));
        AddMarker(textView, layer, new SnapshotSpan(snapshot, matchPosition, 1));
    }

    private static void AddMarker(IWpfTextView textView, IAdornmentLayer layer, SnapshotSpan span)
    {
        if (!textView.TextViewLines.IntersectsBufferSpan(span))
        {
            return;
        }

        var geometry = textView.TextViewLines.GetTextMarkerGeometry(span);
        if (geometry is null)
        {
            return;
        }

        var marker = new Path
        {
            Data = geometry,
            Fill = MarkerBrush,
        };
        // The geometry is in text-rendering coordinates; owner-controlled placement with an
        // explicit viewport offset (markers are rebuilt on every caret/layout change).
        Canvas.SetLeft(marker, -textView.ViewportLeft);
        Canvas.SetTop(marker, -textView.ViewportTop);
        layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, span, tag: null, marker, removedCallback: null);
    }

    private static int FindMatch(ITextSnapshot snapshot, int position, char open, char close, bool forward)
    {
        int depth = 0;
        int step = forward ? 1 : -1;
        for (int i = position; i >= 0 && i < snapshot.Length; i += step)
        {
            char current = snapshot[i];
            if (current == open)
            {
                depth++;
            }
            else if (current == close && --depth == 0)
            {
                return i;
            }
        }

        return -1;
    }
}
