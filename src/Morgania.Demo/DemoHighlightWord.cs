namespace Microsoft.VisualStudio.Demo;

using System.Composition;

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Highlights every occurrence of the word at the caret (VS's "highlight references"
/// look): <see cref="ITextStructureNavigator"/> supplies the word extent,
/// <see cref="ITextSearchService"/> finds the matches, and the markers render on this
/// component's own adornment layer, ordered between the selection and the text markers.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("code")]
public sealed class HighlightWordListener : IWpfTextViewCreationListener
{
    public const string LayerName = "DemoHighlightWord";

    private static readonly IBrush FillBrush = new SolidColorBrush(Color.FromArgb(0x48, 0x26, 0x4F, 0x78));
    private static readonly IBrush StrokeBrush = new SolidColorBrush(Color.FromArgb(0x90, 0x45, 0x6F, 0x9A));

    private readonly ITextSearchService _textSearch;
    private readonly ITextStructureNavigatorSelectorService _navigatorSelector;

    [ImportingConstructor]
    public HighlightWordListener(
        ITextSearchService textSearch,
        ITextStructureNavigatorSelectorService navigatorSelector)
    {
        _textSearch = textSearch;
        _navigatorSelector = navigatorSelector;
    }

    [Export]
    [Name(LayerName)]
    [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.TextMarker)]
    public AdornmentLayerDefinition? HighlightWordLayer { get; }

    public void TextViewCreated(IWpfTextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        var navigator = _navigatorSelector.GetTextStructureNavigator(textView.TextBuffer);
        textView.Caret.PositionChanged += (_, _) => Update(textView, navigator);
        textView.LayoutChanged += (_, _) => Update(textView, navigator);
    }

    private void Update(IWpfTextView textView, ITextStructureNavigator navigator)
    {
        if (textView.IsClosed || textView.InLayout)
        {
            return;
        }

        var layer = textView.GetAdornmentLayer(LayerName);
        layer.RemoveAllAdornments();

        var caret = textView.Caret.Position.BufferPosition;
        if (caret.Position >= caret.Snapshot.Length)
        {
            return;
        }

        var extent = navigator.GetExtentOfWord(caret);
        string word = extent.Span.GetText();
        if (!extent.IsSignificant
            || word.Length < 2
            || !word.All(static c => char.IsLetterOrDigit(c) || c == '_'))
        {
            return;
        }

        var matches = _textSearch.FindAll(
            new FindData(word, caret.Snapshot, FindOptions.WholeWord | FindOptions.MatchCase, navigator));
        foreach (var match in matches)
        {
            if (!textView.TextViewLines.IntersectsBufferSpan(match)
                || textView.TextViewLines.GetTextMarkerGeometry(match) is not { } geometry)
            {
                continue;
            }

            var marker = new Path
            {
                Data = geometry,
                Fill = FillBrush,
                Stroke = StrokeBrush,
                StrokeThickness = 1.0,
            };
            Canvas.SetLeft(marker, -textView.ViewportLeft);
            Canvas.SetTop(marker, -textView.ViewportTop);
            layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, match, tag: null, marker, removedCallback: null);
        }
    }
}
