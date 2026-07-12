using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.GeometryTests;

/// <summary>
/// ITextViewLine geometry is contractual: character bounds, x↔position mapping,
/// word-wrap line identity, and bidi behavior are asserted as invariants over LTR, RTL
/// (Hebrew), and mixed-direction fixtures. (Serialized golden files need an embedded test
/// font to be machine-independent; tracked in docs/progress.md.)
/// </summary>
[TestClass]
public sealed class GeometryTests
{
    private const string LtrLine = "public sealed class Greeter { }";
    private const string RtlLine = "שלום עולם, זהו טקסט בעברית";
    private const string MixedLine = "var mixed = \"bidi: שלום עולם inside\"; // הערה";

    [TestMethod]
    public async Task CharacterBoundsRoundTripOnLtrText()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView(LtrLine);
            var line = view.TextViewLines[0];

            double previousTrailing = double.NegativeInfinity;
            for (int i = 0; i < LtrLine.Length; i++)
            {
                var position = new SnapshotPoint(view.TextSnapshot, i);
                var bounds = line.GetCharacterBounds(position);
                Assert.IsFalse(bounds.IsRightToLeft, $"LTR character {i} reported RTL bounds.");
                Assert.IsTrue(bounds.Width >= 0.0);

                // LTR text: leading edges are non-decreasing in logical order.
                Assert.IsTrue(bounds.Leading >= previousTrailing - 0.01, $"Bounds of character {i} regressed.");
                previousTrailing = bounds.Leading;

                if (bounds.Width > 0.0)
                {
                    double mid = (bounds.Left + bounds.Right) / 2.0;
                    var roundTripped = line.GetBufferPositionFromXCoordinate(mid);
                    Assert.IsNotNull(roundTripped, $"No position found at the middle of character {i}.");
                    Assert.AreEqual(i, roundTripped.Value.Position, $"x→position round trip failed for character {i}.");
                }
            }

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RtlTextRunsRightToLeft()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView(RtlLine);
            var line = view.TextViewLines[0];

            var firstBounds = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, 0));
            var lastBounds = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, RtlLine.Length - 1));

            Assert.IsTrue(firstBounds.IsRightToLeft, "The first Hebrew character must report RTL bounds.");
            Assert.IsTrue(
                firstBounds.Leading > lastBounds.Leading,
                "In an RTL run, the logically first character must be to the right of the logically last.");

            // The leading edge of an RTL character lies to the right of its trailing edge.
            Assert.IsTrue(firstBounds.Leading > firstBounds.Trailing);

            // x→position: the middle of each character must map back to it.
            for (int i = 0; i < RtlLine.Length; i++)
            {
                var bounds = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, i));
                if (bounds.Width > 0.0)
                {
                    double mid = (bounds.Left + bounds.Right) / 2.0;
                    var roundTripped = line.GetBufferPositionFromXCoordinate(mid);
                    Assert.IsNotNull(roundTripped);
                    Assert.AreEqual(i, roundTripped.Value.Position, $"x→position round trip failed for RTL character {i}.");
                }
            }

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MixedDirectionBoundsCoverTheLine()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView(MixedLine);
            var line = view.TextViewLines[0];

            // Every character's mid-x maps back to it, in both directions.
            for (int i = 0; i < MixedLine.Length; i++)
            {
                var bounds = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, i));
                if (bounds.Width > 0.0)
                {
                    double mid = (bounds.Left + bounds.Right) / 2.0;
                    var roundTripped = line.GetBufferPositionFromXCoordinate(mid);
                    Assert.IsNotNull(roundTripped);
                    Assert.AreEqual(i, roundTripped.Value.Position, $"Round trip failed at {i} ('{MixedLine[i]}').");
                }
            }

            // Normalized bounds over the whole extent tile the text width (possibly disjoint runs).
            var allBounds = line.GetNormalizedTextBounds(line.Extent);
            double coveredWidth = allBounds.Sum(bounds => Math.Abs(bounds.Width));
            Assert.AreEqual(line.TextWidth, coveredWidth, 1.0, "Normalized bounds must cover the text width.");

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WordWrapProducesDenseDisjointRows()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string longLine = string.Join(' ', Enumerable.Repeat("wrapping words flow onward", 20));
            var view = HeadlessEditor.CreateView(longLine, width: 300.0, wordWrap: true);
            var rows = view.TextViewLines;

            Assert.IsTrue(rows.Count > 1, "The long line must wrap into multiple rows.");
            Assert.IsTrue(rows[0].IsFirstTextViewLineForSnapshotLine);
            Assert.IsFalse(rows[0].IsLastTextViewLineForSnapshotLine);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (i > 0)
                {
                    // Dense and disjoint: each row starts where the previous ended.
                    Assert.AreEqual(rows[i - 1].EndIncludingLineBreak, row.Start, $"Row {i} is not contiguous.");
                    Assert.AreEqual(rows[i - 1].Bottom, row.Top, 0.01, $"Row {i} is not stacked below row {i - 1}.");
                }

                if (row.End.Position < longLine.Length)
                {
                    Assert.AreEqual(0, row.LineBreakLength, "Wrapped rows have no line break.");
                }
            }

            // The rows partition the entire line.
            int totalLength = rows.Sum(row => row.LengthIncludingLineBreak);
            Assert.AreEqual(longLine.Length, totalLength, "Wrapped rows must partition the snapshot line.");

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LayoutIsDenseAndFillsTheViewport()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 200).Select(i => $"line {i} of the fixture"));
            var view = HeadlessEditor.CreateView(text, height: 300.0);
            var lines = view.TextViewLines;

            Assert.IsTrue(lines[0].Top <= view.ViewportTop, "The first line must start at or above the viewport top.");
            Assert.IsTrue(lines[^1].Bottom >= view.ViewportBottom, "The last line must end at or below the viewport bottom.");

            for (int i = 1; i < lines.Count; i++)
            {
                Assert.AreEqual(lines[i - 1].EndIncludingLineBreak, lines[i].Start, $"Line {i} is not contiguous.");
            }

            // Viewport-only formatting: a 200-line buffer must not be fully formatted for a 300px viewport.
            Assert.IsTrue(lines.Count < 60, $"Expected viewport-only formatting, but {lines.Count} lines were formatted.");

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ScrollingMovesTheViewport()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 200).Select(i => $"line {i}"));
            var view = HeadlessEditor.CreateView(text, height: 300.0);

            var before = view.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber;
            view.ViewScroller.ScrollViewportVerticallyByPixels(-3.0 * view.LineHeight);
            var after = view.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber;
            Assert.AreEqual(before + 3, after, "Scrolling down three line heights must advance three lines.");

            // Scrolling above the buffer start clamps.
            view.ViewScroller.ScrollViewportVerticallyByPixels(1000.0);
            Assert.AreEqual(0, view.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber);
            Assert.IsTrue(view.TextViewLines[0].Top <= view.ViewportTop + 0.01, "No gap above the first line.");

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ScrollingPastTheEndClampsWithTheLastLineAtTheTop()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // The trailing newline gives the buffer a final empty line. Field bug: the
            // fill-downward loop compared positions against the snapshot length, never
            // formatted that line, and without the end of the buffer in the layout the
            // over-scroll clamp could not engage — the view scrolled endlessly.
            string text = string.Join('\n', Enumerable.Range(0, 200).Select(i => $"line {i}")) + "\n";
            var view = HeadlessEditor.CreateView(text, height: 300.0);

            for (int i = 0; i < 100; i++)
            {
                view.ViewScroller.ScrollViewportVerticallyByPixels(-3.0 * view.LineHeight);
            }

            var last = view.TextViewLines[^1];
            Assert.AreEqual(view.TextSnapshot.Length, last.Start.Position, "The final empty line is in the layout.");
            Assert.AreEqual(view.ViewportTop, last.Top, 0.01, "Over-scroll stops with the last line still visible at the top of the viewport.");

            // Scrolling back up recovers normally.
            view.ViewScroller.ScrollViewportVerticallyByPixels(3.0 * view.LineHeight);
            Assert.IsTrue(
                view.TextViewLines.FirstVisibleLine.Start.Position < view.TextSnapshot.Length,
                "Scrolling up from the clamp works.");

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TabsAdvanceToColumnTabStops()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // VS semantics (M5 formatter seam): a tab advances to the next multiple of
            // tabSize * columnWidth from the line's start, not by a fixed increment.
            var view = HeadlessEditor.CreateView("a\tb");
            var line = view.TextViewLines[0];
            var source = view.FormattedLineSource;
            double tabStop = source.TabSize * source.ColumnWidth;

            var boundsOfA = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, 0));
            var boundsOfTab = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, 1));
            var boundsOfB = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, 2));
            Assert.AreEqual(tabStop, boundsOfB.Leading - line.TextLeft, 0.01, "'b' sits at the first tab stop.");
            Assert.AreEqual(tabStop - boundsOfA.Width, boundsOfTab.Width, 0.01, "The tab's bounds reach the stop.");
            view.Close();

            // Leading tabs land on exact stops; text after a stop-aligned prefix keeps
            // advancing a full stop.
            view = HeadlessEditor.CreateView("\t\tx");
            line = view.TextViewLines[0];
            var boundsOfX = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, 2));
            Assert.AreEqual(2 * tabStop, boundsOfX.Leading - line.TextLeft, 0.01, "'x' sits at the second tab stop.");
            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EmptyAndBreakLinesHaveSaneGeometry()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("first\n\nthird");
            var lines = view.TextViewLines;

            Assert.AreEqual(3, lines.Count);
            var empty = lines[1];
            Assert.AreEqual(0, empty.Length);
            Assert.AreEqual(1, empty.LineBreakLength);
            Assert.AreEqual(0.0, empty.TextWidth);
            Assert.IsTrue(empty.Height > 0.0, "An empty line still has the nominal line height.");
            Assert.IsTrue(empty.EndOfLineWidth > 0.0, "A line with a break has an end-of-line box.");

            // The line-break box is addressable.
            var breakBounds = empty.GetCharacterBounds(empty.End);
            Assert.AreEqual(empty.TextRight, breakBounds.Leading, 0.01);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SurrogatePairsAreOneTextElement()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            const string text = "a\U0001F600b"; // emoji = surrogate pair
            var view = HeadlessEditor.CreateView(text);
            var line = view.TextViewLines[0];

            var span = line.GetTextElementSpan(new SnapshotPoint(view.TextSnapshot, 1));
            Assert.AreEqual(2, span.Length, "A surrogate pair is a single text element.");
            Assert.AreEqual(1, span.Start.Position);

            view.Close();
        }).ConfigureAwait(false);
    }
}
