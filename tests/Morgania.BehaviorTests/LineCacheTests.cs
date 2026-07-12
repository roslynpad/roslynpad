using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// M5 line cache: while the line source is unchanged, layouts reuse the
/// published rows — scrolling translates surviving lines instead of reformatting them —
/// and the LayoutChanged args classify lines per the TextViewLineChange contract.
/// </summary>
[TestClass]
public sealed class LineCacheTests
{
    [TestMethod]
    public async Task ScrollingReusesFormattedLinesAndClassifiesChanges()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 100).Select(i => $"line {i}"));
            var view = HeadlessEditor.CreateView(text, height: 200.0);
            var initialLines = view.TextViewLines.ToDictionary(line => line.Start.Position, line => line.IdentityTag);

            TextViewLayoutChangedEventArgs? args = null;
            view.LayoutChanged += (_, e) => args = e;

            view.DisplayTextLineContainingBufferPosition(
                view.TextSnapshot.GetLineFromLineNumber(2).Start, 0.0, ViewRelativePosition.Top);

            foreach (var line in view.TextViewLines)
            {
                if (initialLines.TryGetValue(line.Start.Position, out var tag))
                {
                    Assert.AreSame(tag, line.IdentityTag, "Scrolling reuses surviving formatted lines.");
                    Assert.AreEqual(TextViewLineChange.Translated, line.Change);
                }
                else
                {
                    Assert.AreEqual(TextViewLineChange.NewOrReformatted, line.Change);
                }
            }

            Assert.IsNotNull(args);
            Assert.IsTrue(args.TranslatedLines.Count > 0, "Surviving lines are translated.");
            Assert.IsTrue(args.NewOrReformattedLines.Count > 0, "Lines scrolled into view are formatted fresh.");
            double lineHeight = view.TextViewLines[0].Height;
            Assert.AreEqual(-2.0 * lineHeight, args.TranslatedLines[0].DeltaY, 0.01, "Translation distance is recorded.");

            // An edit produces a new snapshot (and so a new line source): nothing reuses.
            view.TextBuffer.Insert(0, "x");
            Assert.IsTrue(
                view.TextViewLines.All(line => line.Change == TextViewLineChange.NewOrReformatted),
                "Edits invalidate the cache.");

            view.Close();
        }).ConfigureAwait(false);
    }
}
