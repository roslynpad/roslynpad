using System.ComponentModel;
using System.Composition;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>A test error: every <see cref="Token"/> is a syntax error.</summary>
[Export(typeof(IViewTaggerProvider))]
[ContentType("text")]
[TagType(typeof(IErrorTag))]
public sealed class TestErrorTaggerProvider : IViewTaggerProvider
{
    public const string Token = "ERRTOKEN";

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        where T : ITag
        => new TestErrorTagger() as ITagger<T>;

    private sealed class TestErrorTagger : ITagger<IErrorTag>
    {
#pragma warning disable CS0067 // Static content: the tags never change after load.
        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;
#pragma warning restore CS0067

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                string text = span.GetText();
                for (int i = text.IndexOf(Token, StringComparison.Ordinal); i >= 0; i = text.IndexOf(Token, i + Token.Length, StringComparison.Ordinal))
                {
                    yield return new TagSpan<IErrorTag>(new SnapshotSpan(span.Start + i, Token.Length), new ErrorTag(PredefinedErrorTypeNames.SyntaxError));
                }
            }
        }
    }
}

/// <summary>
/// The minimap: a margin that takes room beside the view, draws the document small with its marks, moves the view by
/// click and drag, previews the code under the pointer, and follows its options (CodeGlance Pro's set).
/// </summary>
[TestClass]
public sealed class MinimapTests
{
    private const double WindowWidth = 800.0;
    private const double WindowHeight = 600.0;

    [TestMethod]
    public void LayoutSlidesALongDocumentUnderTheMarginLikeAThumbOnItsTrack()
    {
        // A short document stands still: the band moves over it.
        var shortLayout = new MinimapLayout(lineCount: 50, firstVisibleLine: 10.0, viewportLines: 30.0, height: 600.0, pixelsPerLine: 3.0, MinimapSizing.Proportional);
        Assert.AreEqual(3.0, shortLayout.Pitch);
        Assert.AreEqual(0.0, shortLayout.Offset);
        Assert.AreEqual(30.0, shortLayout.BandTop, 1e-9);
        Assert.AreEqual(10.0, shortLayout.FirstLineForBandTop(30.0), 1e-9);

        // A long one shows its top at the top and its bottom at the bottom.
        var top = new MinimapLayout(1000, 0.0, 30.0, 600.0, 3.0, MinimapSizing.Proportional);
        Assert.AreEqual(0.0, top.Offset);
        var bottom = new MinimapLayout(1000, 970.0, 30.0, 600.0, 3.0, MinimapSizing.Proportional);
        Assert.AreEqual(2400.0, bottom.Offset, 1e-9);
        Assert.AreEqual(600.0 - 90.0, bottom.BandTop, 1e-9);
        Assert.AreEqual(999, bottom.LastDrawnLine);

        // In between, the band's top and the first line solve each other.
        var middle = new MinimapLayout(1000, 485.0, 30.0, 600.0, 3.0, MinimapSizing.Proportional);
        Assert.AreEqual(485.0, middle.FirstLineForBandTop(middle.BandTop), 1e-9);
        Assert.AreEqual(485, middle.LineAt(middle.BandTop + 1.0));

        // Fit scales a long document down to the margin, Fill spans the margin with any document.
        Assert.AreEqual(0.6, new MinimapLayout(1000, 0.0, 30.0, 600.0, 3.0, MinimapSizing.Fit).Pitch, 1e-9);
        Assert.AreEqual(3.0, new MinimapLayout(50, 0.0, 30.0, 600.0, 3.0, MinimapSizing.Fit).Pitch, 1e-9);
        Assert.AreEqual(12.0, new MinimapLayout(50, 0.0, 30.0, 600.0, 3.0, MinimapSizing.Fill).Pitch, 1e-9);
    }

    [TestMethod]
    public void LineDiffMarksAddedLinesAndChangedOnes()
    {
        const LineChangeKind N = LineChangeKind.None;
        const LineChangeKind A = LineChangeKind.Added;
        const LineChangeKind M = LineChangeKind.Modified;
        string[] reference = ["a", "b", "c", "d", "e"];

        CollectionAssert.AreEqual(new[] { N, N, N, N, N }, LineDiff.Compute(reference, reference));
        CollectionAssert.AreEqual(new[] { N, N, A, A, N, N, N }, LineDiff.Compute(reference, ["a", "b", "x", "y", "c", "d", "e"]), "inserted lines");
        CollectionAssert.AreEqual(new[] { N, N, M, N, N }, LineDiff.Compute(reference, ["a", "b", "C", "d", "e"]), "a changed line");
        CollectionAssert.AreEqual(new[] { N, N, M, M, N, N }, LineDiff.Compute(reference, ["a", "b", "C1", "C2", "d", "e"]), "two lines for one");
        CollectionAssert.AreEqual(new[] { N, N, N, N }, LineDiff.Compute(reference, ["a", "b", "c", "e"]), "a removed line marks nothing");
        CollectionAssert.AreEqual(new[] { M, N, N, A, N, N }, LineDiff.Compute(reference, ["A", "b", "c", "x", "d", "e"]), "two hunks apart");
        CollectionAssert.AreEqual(new[] { A, A, A }, LineDiff.Compute([], ["x", "y", "z"]), "everything is new");
    }

    [TestMethod]
    public void ColourOptionsReadWithAndWithoutAlpha()
    {
        Assert.AreEqual(Color.FromArgb(0x40, 0xFF, 0x00, 0x00), MinimapPalette.ParseColor("#FF0000", translucentAlpha: 0x40));
        Assert.AreEqual(Color.FromArgb(0x80, 0x00, 0xFF, 0x00), MinimapPalette.ParseColor("#8000FF00", translucentAlpha: 0x40));
        Assert.IsNull(MinimapPalette.ParseColor(string.Empty, 0x40));
        Assert.IsNull(MinimapPalette.ParseColor("#12345", 0x40));
        Assert.IsNull(MinimapPalette.ParseColor("not a colour", 0x40));
    }

    [TestMethod]
    public async Task MinimapTakesTheRoomBesideTheViewAndGivesItBackWhenOff()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(200));
            try
            {
                var right = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Right)!;
                var bottom = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Bottom)!;
                var scrollBar = host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!;

                Assert.IsTrue(minimap.Enabled);
                Assert.IsTrue(minimap.IsVisible);
                Assert.AreEqual(110.0, minimap.Bounds.Width, 0.01);
                Assert.AreEqual(2, Grid.GetColumn(right.VisualElement), "the right container stands beside the view");
                Assert.AreEqual(0.0, bottom.VisualElement.Margin.Right, 0.01);
                double minimapLeft = LeftInWindow(minimap, window);
                Assert.AreEqual(minimapLeft, RightInWindow(view.VisualElement, window), 0.5, "the text ends where the minimap begins");
                Assert.AreEqual(WindowWidth, minimapLeft + minimap.Bounds.Width + scrollBar.VisualElement.Bounds.Width, 0.5);

                view.Options.SetOptionValue(MinimapOptions.EnabledId, false);
                Dispatcher.UIThread.RunJobs();
                Assert.IsFalse(minimap.IsVisible);
                Assert.AreEqual(1, Grid.GetColumn(right.VisualElement), "without the minimap the scroll bar floats over the text again");
                Assert.AreEqual(scrollBar.VisualElement.Bounds.Width, bottom.VisualElement.Margin.Right, 0.01);
                Assert.AreEqual(WindowWidth, RightInWindow(view.VisualElement, window), 0.5);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AlignmentMovesTheMinimapToTheLeftOfTheOtherMargins()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(100));
            try
            {
                var leftMinimap = (MinimapMargin)host.GetTextViewMargin(MinimapMarginNames.LeftMinimap)!;
                Assert.IsFalse(leftMinimap.IsVisible);

                view.Options.SetOptionValue(MinimapOptions.AlignmentId, MinimapAlignment.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(leftMinimap.IsVisible);
                Assert.IsFalse(minimap.IsVisible);
                Assert.AreEqual(0.0, LeftInWindow(leftMinimap, window), 0.5, "outside the other left margins");
                var right = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Right)!;
                Assert.AreEqual(1, Grid.GetColumn(right.VisualElement));
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ClickJumpsToTheCodeAndDraggingTheBandScrolls()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(400));
            try
            {
                var layout = minimap.Layout;
                Assert.IsTrue(layout.ContentHeight > layout.Height, "the probe needs a document taller than the minimap");

                // A click puts the clicked line in the middle of the view and the caret on it.
                var click = InWindow(minimap, new Point(20.0, layout.YOf(150) + (layout.Pitch / 2.0)), window);
                window.MouseDown(click, MouseButton.Left);
                window.MouseUp(click, MouseButton.Left);
                Assert.AreEqual(150, view.Caret.Position.BufferPosition.GetContainingLine().LineNumber);
                int first = FirstVisibleLine(view);
                int last = view.TextViewLines.LastVisibleLine.Start.GetContainingLine().LineNumber;
                Assert.IsTrue(first < 150 && last > 150, $"line 150 is in the middle of the view ({first}..{last})");
                Assert.AreEqual(150.0, (first + last) / 2.0, 2.0);

                // Only moving leaves the caret where it is.
                view.Options.SetOptionValue(MinimapOptions.MoveOnlyId, true);
                layout = minimap.Layout;
                click = InWindow(minimap, new Point(20.0, layout.YOf(40) + (layout.Pitch / 2.0)), window);
                window.MouseDown(click, MouseButton.Left);
                window.MouseUp(click, MouseButton.Left);
                Assert.AreEqual(150, view.Caret.Position.BufferPosition.GetContainingLine().LineNumber);

                // Dragging the band moves the view with it.
                layout = minimap.Layout;
                int before = FirstVisibleLine(view);
                var grab = InWindow(minimap, new Point(50.0, layout.BandTop + (layout.BandHeight / 2.0)), window);
                window.MouseDown(grab, MouseButton.Left);
                var drop = new Point(grab.X, grab.Y + 60.0);
                window.MouseMove(drop);
                window.MouseUp(drop, MouseButton.Left);
                int after = FirstVisibleLine(view);
                Assert.IsTrue(after > before, $"dragging down scrolls down ({before} -> {after})");
                Assert.AreEqual(before + (60.0 * layout.MaxFirstLine / (layout.Height - layout.BandHeight)), after, 2.0);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PictureInksTheTextAndMarksTheFindMatches()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(
                "xxxxxxxxxxxx\n\nneedle here\nplain",
                configure: options => options.SetOptionValue(MinimapOptions.MarkupFullLineId, true));
            try
            {
                minimap.RasterizeForTest();
                var picture = minimap.Picture;
                var layout = minimap.Layout;
                double scale = picture.Width / minimap.Bounds.Width;
                int RowOf(int line) => (int)((layout.YOf(line) + (layout.Pitch / 3.0)) * scale);
                int textColumn = (int)((MinimapMargin.Inset + 3.0) * scale);
                Assert.IsTrue(picture.GetPixel(textColumn, RowOf(0)).A > 0, "a line of text is inked");
                Assert.AreEqual(0, picture.GetPixel(textColumn, RowOf(1)).A, "an empty line is not");

                var panel = FindReplacePanel.Get(view)!;
                panel.Show();
                panel.SearchText = "needle";
                Dispatcher.UIThread.RunJobs(); // a text box reports its change after it
                Assert.AreEqual(1, panel.Matches.Count);
                minimap.RasterizeForTest();
                picture = minimap.Picture;
                var mark = picture.GetPixel(picture.Width - 2, RowOf(2));
                Assert.IsTrue(mark.A > 0 && mark.R > mark.B, $"the match's line is marked across the minimap ({mark})");
                Assert.AreEqual(0, picture.GetPixel(picture.Width - 2, RowOf(3)).A, "other lines are not");

                panel.Hide();
                Assert.AreEqual(0, panel.Matches.Count);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoverPreviewsTheCodeUnderThePointerAcrossThePage()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(300), configure: options =>
            {
                options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
                options.SetOptionValue(MinimapOptions.PreviewDelayId, 0);
            });
            try
            {
                var layout = minimap.Layout;
                window.MouseMove(InWindow(minimap, new Point(40.0, layout.YOf(120) + (layout.Pitch / 2.0)), window));
                Dispatcher.UIThread.RunJobs();
                var preview = minimap.Preview;
                Assert.IsNotNull(preview);
                Assert.IsTrue(preview.IsOpen);
                Assert.AreEqual(120, preview.CenterLine);

                // As wide as the page beside the minimap, gutter included...
                Assert.AreEqual(LeftInWindow(host.HostControl, window), LeftInWindow(preview.Root, window), 0.5);
                Assert.AreEqual(LeftInWindow(minimap, window) - MinimapPreview.Gap, RightInWindow(preview.Root, window), 0.5);

                // ...with its numbers and its text where the editor's own stand.
                var rows = ((StackPanel)((Border)preview.Root).Child!).Children;
                Assert.AreEqual(10, rows.Count);
                var first = (Panel)rows[0];
                var number = (TextBlock)first.Children[0];
                Assert.AreEqual("116", number.Text);
                var lineNumbers = host.GetTextViewMargin(PredefinedMarginNames.LineNumber)!;
                Assert.AreEqual(RightInWindow(lineNumbers.VisualElement, window) - LineNumberMarginProvider.NumberInset, RightInWindow(number, window), 0.5);
                Assert.AreEqual(LeftInWindow(view.VisualElement, window), LeftInWindow(first.Children[1], window), 0.5);

                window.MouseMove(new Point(100.0, 100.0));
                Assert.IsFalse(preview.IsOpen, "leaving the minimap closes the preview");

                view.Options.SetOptionValue(MinimapOptions.ShowPreviewId, false);
                window.MouseMove(InWindow(minimap, new Point(40.0, layout.YOf(120)), window));
                Assert.IsFalse(preview.IsOpen);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PreviewWaitsForThePointerToRestOffTheViewportBand()
    {
        await HeadlessEditor.RunAsync(async () =>
        {
            var (view, host, window, minimap) = Open(Lines(300), configure: options => options.SetOptionValue(MinimapOptions.PreviewDelayId, 100));
            try
            {
                var layout = minimap.Layout;
                window.MouseMove(InWindow(minimap, new Point(40.0, layout.YOf(150) + (layout.Pitch / 2.0)), window));
                Assert.IsFalse(minimap.Preview is { IsOpen: true }, "not at once");
                await Task.Delay(400).ConfigureAwait(true);
                Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(minimap.Preview is { IsOpen: true }, "once the pointer has rested");

                // The band stands for what is on screen already.
                window.MouseMove(InWindow(minimap, new Point(40.0, layout.BandTop + (layout.BandHeight / 2.0)), window));
                Assert.IsFalse(minimap.Preview!.IsOpen);
                await Task.Delay(400).ConfigureAwait(true);
                Dispatcher.UIThread.RunJobs();
                Assert.IsFalse(minimap.Preview.IsOpen, "resting on the band previews nothing");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoPreviewOpensBelowTheDocumentsLastLine()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(20), configure: options => options.SetOptionValue(MinimapOptions.PreviewDelayId, 0));
            try
            {
                var layout = minimap.Layout;
                double below = Math.Max(layout.YOf(layout.LineCount), layout.BandTop + layout.BandHeight) + 10.0;
                Assert.IsTrue(below < minimap.Bounds.Height, "the probe needs room below the document and the band");

                window.MouseMove(InWindow(minimap, new Point(40.0, below), window));
                Assert.IsFalse(minimap.Preview is { IsOpen: true }, "below the last line there is nothing to show");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WrappedTheBandStillCoversTheRoomBelowAShortDocument()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(10), configure: options =>
                options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.WordWrap));
            try
            {
                minimap.RasterizeForTest();
                var layout = minimap.Layout;
                Assert.AreEqual(view.ViewportHeight / view.LineHeight * layout.Pitch, layout.BandHeight, layout.Pitch,
                    "the band spans the viewport, the room below the ten lines included");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HidingTheOriginalScrollBarMakesTheMinimapTheScrollBar()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(100), configure: options => options.SetOptionValue(MinimapOptions.HideOriginalScrollBarId, true));
            try
            {
                var scrollBar = host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!;
                Assert.IsFalse(scrollBar.VisualElement.IsVisible);
                Assert.AreEqual(WindowWidth, RightInWindow(minimap, window), 0.5);

                view.Options.SetOptionValue(MinimapOptions.EnabledId, false);
                Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(scrollBar.VisualElement.IsVisible, "the scroll bar comes back with the minimap gone");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LineCountLimitsEmptyTheMinimapOrStepItAside()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(50), configure: options => options.SetOptionValue(MinimapOptions.MaxLineCountId, 10));
            try
            {
                var right = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Right)!;
                Assert.IsTrue(minimap.IsVisible, "beyond the limit the minimap keeps its room by default");
                Assert.IsFalse(minimap.DrawsDocument);
                Assert.AreEqual(2, Grid.GetColumn(right.VisualElement));

                view.Options.SetOptionValue(MinimapOptions.EmptyOutOfRangeId, false);
                Assert.IsFalse(minimap.IsVisible);
                Assert.AreEqual(1, Grid.GetColumn(right.VisualElement));

                view.Options.SetOptionValue(MinimapOptions.MaxLineCountId, 0);
                Assert.IsTrue(minimap.IsVisible);
                Assert.IsTrue(minimap.DrawsDocument);

                view.Options.SetOptionValue(MinimapOptions.MinLineCountId, 60);
                Assert.IsFalse(minimap.IsVisible, "below the lower limit likewise");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OnlyTheHostTurnsTheMinimapOff()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(100));
            try
            {
                // Neither a click in its corner nor a key hides it: that is the host's, through the option.
                var corner = InWindow(minimap, new Point(minimap.Bounds.Width - 6.0, 6.0), window);
                window.MouseMove(corner);
                window.MouseDown(corner, MouseButton.Left);
                window.MouseUp(corner, MouseButton.Left);
                view.VisualElement.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.G,
                    KeyModifiers = KeyModifiers.Control | KeyModifiers.Shift,
                });
                Assert.IsTrue(view.Options.GetOptionValue(MinimapOptions.EnabledId));
                Assert.IsTrue(minimap.IsVisible);

                view.Options.SetOptionValue(MinimapOptions.EnabledId, false);
                Assert.IsFalse(minimap.IsVisible);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InnerEdgeResizesUnlessTheWidthIsLocked()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(100));
            try
            {
                view.Options.SetOptionValue(MinimapOptions.WidthId, 1000.0);
                Assert.AreEqual(400.0, view.Options.GetOptionValue(MinimapOptions.WidthId), "the width is clamped");
                view.Options.SetOptionValue(MinimapOptions.WidthId, 110.0);
                Dispatcher.UIThread.RunJobs();

                DragGrip(minimap, window, -50.0);
                Assert.AreEqual(160.0, view.Options.GetOptionValue(MinimapOptions.WidthId), 0.01);

                view.Options.SetOptionValue(MinimapOptions.LockWidthId, true);
                Dispatcher.UIThread.RunJobs();
                DragGrip(minimap, window, -50.0);
                Assert.AreEqual(160.0, view.Options.GetOptionValue(MinimapOptions.WidthId), 0.01);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ResizingKeepsTheScrollRangeAndWritesTheWidthOnce()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 200).Select(i => new string('x', 40 + (i % 5 * 40))));
            var (view, host, window, minimap) = Open(text);
            try
            {
                var across = (ScrollBar)host.GetTextViewMargin(PredefinedMarginNames.HorizontalScrollBar)!.VisualElement;
                Assert.IsTrue(across.Maximum > 0.0, "the probe needs lines wider than the view");
                int writes = 0;
                view.Options.OptionChanged += (_, e) => writes += e.OptionId == MinimapOptions.WidthOptionName ? 1 : 0;
                var ranges = new List<double>();
                across.PropertyChanged += (_, e) =>
                {
                    if (e.Property == RangeBase.MaximumProperty)
                    {
                        ranges.Add((double)e.NewValue!);
                    }
                };

                var grip = InWindow(minimap, new Point(1.0, 200.0), window);
                window.MouseDown(grip, MouseButton.Left);
                for (int step = 1; step <= 5; step++)
                {
                    window.MouseMove(new Point(grip.X - (step * 7.0), grip.Y));
                    Dispatcher.UIThread.RunJobs();
                }

                Assert.AreEqual(0, writes, "the option waits for the drag to end");
                Assert.AreEqual(145.0, minimap.Bounds.Width, 0.01, "the minimap follows the edge meanwhile");
                window.MouseUp(new Point(grip.X - 35.0, grip.Y), MouseButton.Left);
                Dispatcher.UIThread.RunJobs();

                Assert.AreEqual(1, writes);
                Assert.AreEqual(145.0, view.Options.GetOptionValue(MinimapOptions.WidthId), 0.01);
                Assert.IsFalse(ranges.Contains(0.0), $"the text never collapses under the horizontal bar ({string.Join(", ", ranges)})");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ShowingOnScrollBarHoverFloatsTheMinimapOverTheText()
    {
        await HeadlessEditor.RunAsync(async () =>
        {
            var (view, host, window, minimap) = Open(Lines(100), configure: options => options.SetOptionValue(MinimapOptions.ShowOnScrollBarHoverId, true));
            try
            {
                var right = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Right)!;
                var scrollBar = host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!;
                Assert.IsFalse(minimap.IsVisible);
                Assert.AreEqual(1, Grid.GetColumn(right.VisualElement));

                // Raised on the bar itself: without a theme the bar has no template to be hit on.
                RaisePointer(scrollBar.VisualElement, InputElement.PointerEnteredEvent, window);
                Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(minimap.IsVisible, "resting on the scroll bar shows the minimap");
                Assert.IsFalse(minimap.ReservesSpace);
                Assert.AreEqual(1, Grid.GetColumn(right.VisualElement), "it floats over the text");
                Assert.AreEqual(WindowWidth, RightInWindow(view.VisualElement, window), 0.5);

                RaisePointer(scrollBar.VisualElement, InputElement.PointerExitedEvent, window);
                await Task.Delay(600).ConfigureAwait(true);
                Dispatcher.UIThread.RunJobs();
                Assert.IsFalse(minimap.IsVisible, "leaving the scroll bar hides it again");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InkFollowsEditsLineByLine()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open(Lines(20));
            try
            {
                minimap.RasterizeForTest();
                var ink = minimap.Ink!;
                var line10 = ink.Get(view.VisualSnapshot, 10, build: false);
                Assert.IsNotNull(line10);

                view.TextBuffer.Insert(view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(2).Start.Position, "inserted\n");
                Assert.AreSame(line10, ink.Get(view.VisualSnapshot, 11, build: false), "an edit keeps the ink of the lines it did not touch");
                Assert.AreEqual("inserted", ink.Get(view.VisualSnapshot, 2, build: true)!.Columns);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ErrorsAreMarkedAcrossTheMinimapOrUnderTheirCharacters()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open($"fine\nhas {TestErrorTaggerProvider.Token} here\nfine");
            try
            {
                minimap.RasterizeForTest();
                var picture = minimap.Picture;
                int errorRow = RowOf(minimap, 1);
                var mark = picture.GetPixel(picture.Width - 2, errorRow);
                Assert.IsTrue(mark.A > 0 && mark.R > mark.G && mark.R > mark.B, $"the error's line is marked in the error colour ({mark})");
                Assert.AreEqual(0, picture.GetPixel(picture.Width - 2, RowOf(minimap, 0)).A);

                view.Options.SetOptionValue(MinimapOptions.ErrorFullLineId, false);
                minimap.RasterizeForTest();
                Assert.AreEqual(0, minimap.Picture.GetPixel(picture.Width - 2, errorRow).A, "without the full line only the characters are marked");

                view.Options.SetOptionValue(MinimapOptions.ErrorFullLineId, true);
                view.Options.SetOptionValue(MinimapOptions.ErrorHighlightId, false);
                minimap.RasterizeForTest();
                Assert.AreEqual(0, minimap.Picture.GetPixel(picture.Width - 2, errorRow).A, "error marks follow their option");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MarkersLabelTheLinesTheyStandOn()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open("#region Setup\ncode\n// MARK: - Section\n/* MARK: Title */\nplain MARK:x");
            try
            {
                minimap.RasterizeForTest();
                var ink = minimap.Ink!;
                var visual = view.VisualSnapshot;
                Assert.AreEqual("Setup", ink.Get(visual, 0, build: true)!.MarkerLabel);
                Assert.IsNull(ink.Get(visual, 1, build: true)!.MarkerLabel);
                Assert.AreEqual("Section", ink.Get(visual, 2, build: true)!.MarkerLabel);
                Assert.AreEqual("Title", ink.Get(visual, 3, build: true)!.MarkerLabel, "without the comment's closing");
                Assert.IsNull(ink.Get(visual, 4, build: true)!.MarkerLabel);

                view.Options.SetOptionValue(MinimapOptions.ShowMarkersId, false);
                Assert.IsNull(ink.Get(visual, 0, build: true)!.MarkerLabel);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GlyphMasksInkEachCharacterWhereItsGlyphDoes()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("x");
            var typeface = HeadlessEditor.Container.GetExport<IClassificationFormatMapService>().GetClassificationFormatMap(view).DefaultTextProperties.Typeface;
            var masks = new MinimapGlyphMasks(typeface);
            static float Upper(float[] mask) => mask.Take(mask.Length / 2).Sum();
            static float Lower(float[] mask) => mask.Skip(mask.Length / 2).Sum();

            var dot = masks.Get('.');
            var quote = masks.Get('\'');
            Assert.IsNotNull(dot);
            Assert.IsNotNull(quote);
            Assert.IsTrue(Upper(dot) < Lower(dot), "a full stop inks the lower half");
            Assert.IsTrue(Upper(quote) > Lower(quote), "an apostrophe inks the upper half");
            Assert.IsNull(masks.Get(' '), "a blank inks nothing");
            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CollapsedRegionsLeaveTheMinimapAsTheViewShowsThem()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var (view, host, window, minimap) = Open("header <<\nhidden one\nhidden two >>\nfooter");
            try
            {
                var manager = HeadlessEditor.Container.GetExport<IOutliningManagerService>().GetOutliningManager(view)!;
                var region = manager.GetAllRegions(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length)).Single();
                Assert.IsNotNull(manager.TryCollapse(region));
                Dispatcher.UIThread.RunJobs();
                minimap.RasterizeForTest();

                var visual = view.VisualSnapshot;
                Assert.AreEqual(2, minimap.Layout.LineCount, "one row per line the view shows");
                Assert.AreEqual(visual.GetLineFromLineNumber(0).GetText(), minimap.Ink!.Get(visual, 0, build: true)!.Columns);
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ChangesSinceTheSavedTextAreMarkedGreenAndBlue()
    {
        await HeadlessEditor.RunAsync(async () =>
        {
            var (view, host, window, minimap) = Open(Lines(40));
            try
            {
                var buffer = view.TextBuffer;
                buffer.Replace(buffer.CurrentSnapshot.GetLineFromLineNumber(5).Extent.Span, "changed");
                buffer.Insert(buffer.CurrentSnapshot.GetLineFromLineNumber(11).Start.Position, "inserted\n");
                await SettledAsync(minimap).ConfigureAwait(true);

                var picture = minimap.Picture;
                var modified = picture.GetPixel(picture.Width - 2, RowOf(minimap, 5));
                var added = picture.GetPixel(picture.Width - 2, RowOf(minimap, 11));
                Assert.IsTrue(modified.A > 0 && modified.B > modified.R && modified.B > modified.G, $"a changed line is blue ({modified})");
                Assert.IsTrue(added.A > 0 && added.G > added.R && added.G > added.B, $"an added line is green ({added})");
                Assert.AreEqual(0, picture.GetPixel(picture.Width - 2, RowOf(minimap, 20)).A, "an untouched line is not marked");

                // Saved, nothing is new any more; changed back, nothing is changed.
                TextChangeBaseline.For(buffer).MarkSaved();
                await SettledAsync(minimap).ConfigureAwait(true);
                Assert.AreEqual(0, minimap.Picture.GetPixel(picture.Width - 2, RowOf(minimap, 5)).A);
                buffer.Replace(buffer.CurrentSnapshot.GetLineFromLineNumber(5).Extent.Span, "again");
                buffer.Replace(buffer.CurrentSnapshot.GetLineFromLineNumber(5).Extent.Span, "changed");
                await SettledAsync(minimap).ConfigureAwait(true);
                Assert.AreEqual(0, minimap.Picture.GetPixel(picture.Width - 2, RowOf(minimap, 5)).A, "an edit undone leaves no mark");

                view.Options.SetOptionValue(MinimapOptions.ChangeHighlightId, false);
                buffer.Insert(0, "new\n");
                await SettledAsync(minimap).ConfigureAwait(true);
                Assert.AreEqual(0, minimap.Picture.GetPixel(picture.Width - 2, RowOf(minimap, 0)).A, "change marks follow their option");
            }
            finally
            {
                Close(host, window);
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheCommittedTextIsTheReferenceWhereTheFileIsInGit()
    {
        string directory = Path.Combine(Path.GetTempPath(), "morgania-minimap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            if (!Git(directory, "init", "-q"))
            {
                Assert.Inconclusive("git cannot be run here");
            }

            string file = Path.Combine(directory, "sample.txt");
            await File.WriteAllTextAsync(file, "one\ntwo\nthree\n").ConfigureAwait(false);
            Assert.IsTrue(Git(directory, "add", "sample.txt"));
            Assert.IsTrue(Git(directory, "-c", "user.name=test", "-c", "user.email=test@example.com", "commit", "-q", "-m", "first"));

            await HeadlessEditor.RunAsync(async () =>
            {
                var view = HeadlessEditor.CreateView("one\nTWO\nthree\n");
                var baseline = TextChangeBaseline.For(view.TextBuffer);
                var raised = new TaskCompletionSource();
                baseline.Changed += (_, _) => raised.TrySetResult();
                Assert.IsFalse(baseline.IsCommitted, "without a file, the text last saved is the reference");

                baseline.FilePath = file;
                await raised.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(true);
                Assert.IsTrue(baseline.IsCommitted, "the committed text is the reference");
                Assert.AreEqual("one\ntwo\nthree\n", baseline.Text.Replace("\r\n", "\n", StringComparison.Ordinal));
                view.Close();
            }).ConfigureAwait(false);
        }
        finally
        {
            foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }

            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task SettledAsync(MinimapMargin minimap)
    {
        await Task.Delay(700).ConfigureAwait(true);
        Dispatcher.UIThread.RunJobs();
        minimap.RasterizeForTest();
    }

    private static bool Git(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(start)!;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static int RowOf(MinimapMargin minimap, int line)
    {
        var layout = minimap.Layout;
        return (int)((layout.YOf(line) + (layout.Pitch / 3.0)) * (minimap.Picture.Width / minimap.Bounds.Width));
    }

    private static (IWpfTextView View, IWpfTextViewHost Host, Window Window, MinimapMargin Minimap) Open(string text, Action<IEditorOptions>? configure = null)
    {
        var view = HeadlessEditor.CreateView(text, WindowWidth, WindowHeight);
        configure?.Invoke(view.Options);
        var host = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>().CreateTextViewHost(view, setFocus: false);
        // A layer manager with an overlay, as a themed window has: the preview opens on it.
        var window = new Window
        {
            Width = WindowWidth,
            Height = WindowHeight,
            Content = new VisualLayerManager { EnableOverlayLayer = true, Child = host.HostControl },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (view, host, window, (MinimapMargin)host.GetTextViewMargin(MinimapMarginNames.Minimap)!);
    }

    private static void Close(IWpfTextViewHost host, Window window)
    {
        window.Close();
        host.Close();
    }

    private static string Lines(int count) => string.Join('\n', Enumerable.Range(0, count).Select(i => $"line {i} with some text"));

    private static int FirstVisibleLine(IWpfTextView view) => view.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber;

    private static Point InWindow(Visual visual, Point point, Window window) => visual.TranslatePoint(point, window)!.Value;

    private static double LeftInWindow(Visual visual, Window window) => InWindow(visual, default, window).X;

    private static double RightInWindow(Visual visual, Window window) => InWindow(visual, new Point(visual.Bounds.Width, 0.0), window).X;

    private static void DragGrip(MinimapMargin minimap, Window window, double dx)
    {
        var grip = InWindow(minimap, new Point(1.0, 200.0), window);
        var drop = new Point(grip.X + dx, grip.Y);
        window.MouseDown(grip, MouseButton.Left);
        window.MouseMove(drop);
        window.MouseUp(drop, MouseButton.Left);
    }

    private static void RaisePointer(Control target, RoutedEvent<PointerEventArgs> routedEvent, Window window)
    {
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
        target.RaiseEvent(new PointerEventArgs(
            routedEvent,
            target,
            pointer,
            window,
            InWindow(target, new Point(2.0, 2.0), window),
            timestamp: 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None));
    }

}
