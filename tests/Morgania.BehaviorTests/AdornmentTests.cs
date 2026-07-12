using System.Composition;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// A test intra-text adornment: every "@@" token is replaced by a fixed-size swatch,
/// exercising the space-negotiation pipeline through contract APIs only (M3 acceptance).
/// </summary>
[Export(typeof(IViewTaggerProvider))]
[ContentType("text")]
[TagType(typeof(IntraTextAdornmentTag))]
public sealed class TestSwatchTaggerProvider : IViewTaggerProvider
{
    public const double SwatchWidth = 40.0;
    public const double SwatchHeight = 10.0;

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        where T : ITag
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(() => new TestSwatchTagger()) as ITagger<T>;
    }

    private sealed class TestSwatchTagger : ITagger<IntraTextAdornmentTag>
    {
        private readonly Dictionary<int, IntraTextAdornmentTag> _tagsByPosition = [];

#pragma warning disable CS0067
        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;
#pragma warning restore CS0067

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                string text = span.GetText();
                for (int index = text.IndexOf("@@", StringComparison.Ordinal); index >= 0; index = text.IndexOf("@@", index + 2, StringComparison.Ordinal))
                {
                    int position = span.Start.Position + index;
                    if (!_tagsByPosition.TryGetValue(position, out var tag))
                    {
                        tag = new IntraTextAdornmentTag(
                            new Rectangle { Width = SwatchWidth, Height = SwatchHeight, Fill = Brushes.Red },
                            removalCallback: null);
                        _tagsByPosition[position] = tag;
                    }

                    yield return new TagSpan<IntraTextAdornmentTag>(new SnapshotSpan(span.Snapshot, position, 2), tag);
                }
            }
        }
    }
}

[TestClass]
public sealed class AdornmentTests
{
    [TestMethod]
    public async Task IntraTextAdornmentNegotiatesSpaceInTheLine()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var plain = HeadlessEditor.CreateView("before  after");
            double plainWidth = plain.TextViewLines[0].TextWidth;
            plain.Close();

            var view = HeadlessEditor.CreateView("before@@after");
            var line = view.TextViewLines[0];

            // The swatch's negotiated width is part of the line.
            Assert.IsTrue(
                line.TextWidth > plainWidth + TestSwatchTaggerProvider.SwatchWidth / 2.0,
                $"Expected the line ({line.TextWidth:F1}) to be wider than plain text ({plainWidth:F1}) by roughly the swatch width.");

            // The adornment is addressable through the contract geometry APIs.
            int adornmentStart = "before".Length;
            var identityTags = line.GetAdornmentTags(view.Properties.PropertyList
                .Select(static pair => pair.Value)
                .OfType<object>()
                .First(static value => value.GetType().Name.Contains("IntraTextAdornmentSupport", StringComparison.Ordinal)));
            Assert.AreEqual(1, identityTags.Count, "The line reports exactly one adornment for the support provider.");

            var bounds = line.GetAdornmentBounds(identityTags[0]);
            Assert.IsNotNull(bounds);
            Assert.AreEqual(TestSwatchTaggerProvider.SwatchWidth, bounds.Value.Width, 1.0);

            // Text after the adornment starts at (or right of) the adornment's trailing edge.
            var afterBounds = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, adornmentStart + 2));
            Assert.IsTrue(afterBounds.Leading >= bounds.Value.Left + TestSwatchTaggerProvider.SwatchWidth - 1.0);

            // Extended character bounds inside the replaced span answer the adornment box.
            var extended = line.GetExtendedCharacterBounds(new SnapshotPoint(view.TextSnapshot, adornmentStart + 1));
            Assert.AreEqual(bounds.Value.Left, extended.Left, 0.01);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AdornmentLayersOrderAndPositionElements()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("line with a marker here");

            // TextMarker renders below the text; Squiggle above it. Both resolve.
            var markerLayer = view.GetAdornmentLayer(PredefinedAdornmentLayers.TextMarker);
            var squiggleLayer = view.GetAdornmentLayer(PredefinedAdornmentLayers.Squiggle);
            Assert.IsTrue(markerLayer.IsEmpty && squiggleLayer.IsEmpty);

            var span = new SnapshotSpan(view.TextSnapshot, 5, 4);
            var geometry = view.TextViewLines.GetTextMarkerGeometry(span);
            Assert.IsNotNull(geometry);

            var marker = new Avalonia.Controls.Shapes.Path { Data = geometry, Fill = Brushes.Yellow };
            Assert.IsTrue(markerLayer.AddAdornment(span, tag: "test-marker", marker));
            Assert.AreEqual(1, markerLayer.Elements.Count);
            Assert.AreEqual(marker, markerLayer.Elements[0].Adornment);

            markerLayer.RemoveAdornmentsByTag("test-marker");
            Assert.IsTrue(markerLayer.IsEmpty);

            // An undeclared layer name is rejected per the contract.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.GetAdornmentLayer("No Such Layer"));

            view.Close();
        }).ConfigureAwait(false);
    }
}
