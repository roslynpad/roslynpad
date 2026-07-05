using System.Composition;

using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// A test outlining region: the span from "&lt;&lt;" through "&gt;&gt;" is collapsible.
/// </summary>
[Export(typeof(ITaggerProvider))]
[ContentType("text")]
[TagType(typeof(IOutliningRegionTag))]
public sealed class TestOutliningTaggerProvider : ITaggerProvider
{
    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
        => new TestOutliningTagger() as ITagger<T>;

    private sealed class TestOutliningTagger : ITagger<IOutliningRegionTag>
    {
#pragma warning disable CS0067 // Static content: the tags never change after load.
        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;
#pragma warning restore CS0067

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            string text = snapshot.GetText();
            int start = text.IndexOf("<<", StringComparison.Ordinal);
            int end = text.IndexOf(">>", StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                yield return new TagSpan<IOutliningRegionTag>(
                    new SnapshotSpan(snapshot, start, end + 2 - start),
                    new OutliningRegionTag(false, false, "...", "collapsed text"));
            }
        }
    }
}

/// <summary>
/// M5 elision: collapsing an outlining region elides its extent from the visual buffer;
/// view lines join across the collapse and answer edit-buffer geometry for hidden text.
/// </summary>
[TestClass]
public sealed class OutliningTests
{
    [TestMethod]
    public async Task CollapsingARegionElidesItFromTheView()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("header <<\nhidden one\nhidden two >>\nfooter");
            var editSnapshot = view.TextSnapshot;
            var manager = HeadlessEditor.Container.GetExport<IOutliningManagerService>().GetOutliningManager(view);
            Assert.IsNotNull(manager, "Structured views get an outlining manager.");

            var region = manager.GetAllRegions(new SnapshotSpan(editSnapshot, 0, editSnapshot.Length)).Single();
            var collapsed = manager.TryCollapse(region);
            Assert.IsNotNull(collapsed, "The test region is collapsible.");

            // The collapse elides the extent from the visual buffer.
            int regionStart = "header ".Length;
            int regionLength = editSnapshot.Length - regionStart - "\nfooter".Length;
            Assert.AreEqual(editSnapshot.Length - regionLength, view.VisualSnapshot.Length);

            // A fresh layout joins header and footer across the collapse.
            view.DisplayTextLineContainingBufferPosition(new SnapshotPoint(editSnapshot, 0), 0.0, ViewRelativePosition.Top);
            var lines = view.TextViewLines;
            var first = lines[0];
            int footerStart = editSnapshot.GetText().IndexOf("footer", StringComparison.Ordinal);

            // The first view line's extent covers the hidden text; the second starts at "footer".
            Assert.IsTrue(first.ContainsBufferPosition(new SnapshotPoint(editSnapshot, regionStart + 5)));
            Assert.AreEqual(footerStart, lines[1].Start.Position);

            // Hidden positions render at the collapse point; the caret treats the hidden
            // region as one element.
            var hiddenBounds = first.GetCharacterBounds(new SnapshotPoint(editSnapshot, regionStart + 5));
            var collapsePointBounds = first.GetCharacterBounds(new SnapshotPoint(editSnapshot, regionStart));
            Assert.AreEqual(collapsePointBounds.Leading, hiddenBounds.Leading, 0.01);
            var elementSpan = first.GetTextElementSpan(new SnapshotPoint(editSnapshot, regionStart + 5));
            Assert.AreEqual(regionStart, elementSpan.Start.Position);
            Assert.AreEqual(regionStart + regionLength, elementSpan.End.Position);

            // Expanding restores the full text and the original line structure.
            manager.Expand(collapsed);
            Assert.AreEqual(editSnapshot.Length, view.VisualSnapshot.Length);
            view.DisplayTextLineContainingBufferPosition(new SnapshotPoint(editSnapshot, 0), 0.0, ViewRelativePosition.Top);
            Assert.AreEqual(4, view.TextViewLines.Count);
            Assert.AreEqual(footerStart, view.TextViewLines[3].Start.Position);

            view.Close();
        }).ConfigureAwait(false);
    }
}
