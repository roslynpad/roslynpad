using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// M4 acceptance: margins are MEF-discovered, ordered within their containers, and
/// removable (option-driven); the vertical scrollbar implements IVerticalScrollBar mapping.
/// </summary>
[TestClass]
public sealed class MarginTests
{
    private static readonly double[] s_narrowerWidths = [350.0, 300.0];

    [TestMethod]
    public async Task MarginsAreDiscoveredOrderedAndRemovable()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 100).Select(i => $"line {i}"));
            var view = HeadlessEditor.CreateView(text, height: 300.0);
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);

            // Discovery: the standard margins resolve by name through the host.
            var lineNumbers = host.GetTextViewMargin(PredefinedMarginNames.LineNumber);
            var glyph = host.GetTextViewMargin(PredefinedMarginNames.Glyph);
            var outlining = host.GetTextViewMargin(PredefinedMarginNames.Outlining);
            var verticalScrollBar = host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar);
            var horizontalScrollBar = host.GetTextViewMargin(PredefinedMarginNames.HorizontalScrollBar);
            Assert.IsNotNull(lineNumbers);
            Assert.IsNotNull(glyph);
            Assert.IsNotNull(outlining);
            Assert.IsNotNull(verticalScrollBar);
            Assert.IsNotNull(horizontalScrollBar);

            // Ordering: within the Left container, Glyph precedes LineNumber precedes
            // Outlining per [Order].
            var leftContainer = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Left)!;
            var panel = (Avalonia.Controls.StackPanel)leftContainer.VisualElement;
            Assert.AreEqual(3, panel.Children.Count);
            Assert.AreEqual(glyph.VisualElement, panel.Children[0], "Glyph margin is ordered before LineNumber.");
            Assert.AreEqual(lineNumbers.VisualElement, panel.Children[1]);
            Assert.AreEqual(outlining.VisualElement, panel.Children[2], "Outlining margin is ordered after LineNumber.");

            // Removable: the line-number margin follows its option (vendored default: off).
            view.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            Assert.IsTrue(lineNumbers.Enabled);
            view.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
            Assert.IsFalse(lineNumbers.Enabled);
            Assert.IsFalse(lineNumbers.VisualElement.IsVisible);
            view.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            Assert.IsTrue(lineNumbers.VisualElement.IsVisible);

            // Unknown margins return null per contract.
            Assert.IsNull(host.GetTextViewMargin("No Such Margin"));

            host.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HorizontalScrollBarDragCeilingMatchesTheViewportClamp()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // One line far wider than the viewport.
            var view = HeadlessEditor.CreateView(new string('x', 400), width: 200.0, height: 100.0);
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var scrollBar = (Avalonia.Controls.Primitives.ScrollBar)
                host.GetTextViewMargin(PredefinedMarginNames.HorizontalScrollBar)!.VisualElement;

            // Scroll as far as the view itself allows (the wheel / keyboard path)...
            view.ViewportLeft = double.MaxValue;

            // ...and the scrollbar's drag range must end exactly there.
            Assert.AreEqual(view.ViewportLeft, scrollBar.Maximum, 0.01,
                "the scrollbar's drag ceiling must be the viewport's own");

            host.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HorizontalScrollBarEndsClearOfTheVerticalScrollBar()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 100).Select(i => $"line {i}"));
            var view = HeadlessEditor.CreateView(text, height: 300.0);
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var window = new Avalonia.Controls.Window { Width = 400, Height = 300, Content = host.HostControl };
            window.Show();

            try
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                // The bottom row must stay clear of the vertical scrollbar's lane.
                var right = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Right)!;
                var bottom = (IWpfTextViewMargin)host.GetTextViewMargin(PredefinedMarginNames.Bottom)!;
                Assert.IsTrue(right.VisualElement.Bounds.Width > 0, "the probe needs the vertical bar's lane laid out");
                Assert.AreEqual(right.VisualElement.Bounds.Width, bottom.VisualElement.Margin.Right, 0.01,
                    "the bottom row must end where the right container begins");
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DisabledScrollBarsStayHiddenWhenTheirRangeChanges()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 100).Select(i => $"line {i} " + new string('x', 120)));
            var view = HeadlessEditor.CreateView(text, height: 300.0, wordWrap: true);
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var window = new Avalonia.Controls.Window { Width = 400, Height = 300, Content = host.HostControl };
            window.Show();

            try
            {
                var horizontal = host.GetTextViewMargin(PredefinedMarginNames.HorizontalScrollBar)!.VisualElement;
                var vertical = host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!.VisualElement;
                view.Options.SetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId, false);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.IsFalse(vertical.IsVisible, "the vertical scroll bar follows its option at once");

                // A new width gives both bars a new range, which a scroll bar answers by showing
                // itself again unless its own Visibility says otherwise.
                foreach (double width in s_narrowerWidths)
                {
                    window.Width = width;
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    Assert.IsFalse(horizontal.IsVisible, "wrapped lines need no horizontal scroll bar");
                    Assert.IsFalse(vertical.IsVisible, "the vertical scroll bar stays off");
                }
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ScrollBarsShowOnlyWhileThereIsSomethingToScrollWhereAsked()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("one\ntwo\nthree", height: 300.0);
            var host = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>().CreateTextViewHost(view, setFocus: false);
            var window = new Avalonia.Controls.Window { Width = 400, Height = 300, Content = host.HostControl };
            window.Show();

            try
            {
                var vertical = host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!.VisualElement;
                var horizontal = host.GetTextViewMargin(PredefinedMarginNames.HorizontalScrollBar)!.VisualElement;
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(vertical.IsVisible, "By default a bar shows whenever its margin is enabled.");

                view.Options.SetOptionValue(TextViewScrollOptions.ScrollBarsOnlyWhenScrollableId, true);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.IsFalse(vertical.IsVisible, "Three lines fit: nothing to scroll.");
                Assert.IsFalse(horizontal.IsVisible, "And none runs past the viewport.");

                view.TextBuffer.Insert(view.TextBuffer.CurrentSnapshot.Length, string.Concat(Enumerable.Range(0, 100).Select(i => $"\nline {i}")));
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(vertical.IsVisible, "A hundred lines more do not fit.");

                view.TextBuffer.Insert(0, new string('x', 400) + "\n");
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(horizontal.IsVisible, "And a line longer than the viewport runs past it.");
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task VerticalScrollBarMapsBufferPositionsAndScrollsTheView()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 200).Select(i => $"line {i}"));
            var view = HeadlessEditor.CreateView(text, height: 300.0);
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);

            var scrollBar = (IVerticalScrollBar)host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!;
            var map = scrollBar.Map;

            // The scroll map is line-linear and round-trips buffer positions.
            var line50 = view.TextSnapshot.GetLineFromLineNumber(50);
            Assert.AreEqual(50.0, map.GetCoordinateAtBufferPosition(line50.Start), 0.01);
            Assert.AreEqual(line50.Start, map.GetBufferPositionAtCoordinate(50.0));
            Assert.AreEqual(0.0, map.Start);
            Assert.AreEqual(199.0, map.End, 0.01);

            // Scrolling the view moves the caret-visible region; the map follows the lines.
            view.DisplayTextLineContainingBufferPosition(line50.Start, 0.0, ViewRelativePosition.Top);
            Assert.AreEqual(50, view.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber);

            host.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVerticalScrollBarOverWrappedLinesHasAThumbThatLeavesTheTrackAndReachesTheEnd()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // a few lines, each far longer than the view is wide, so the document is many viewports tall
            string text = string.Join('\n', Enumerable.Range(0, 8).Select(i => $"line {i} " + string.Join(' ', Enumerable.Repeat("word", 400))));
            var view = HeadlessEditor.CreateView(text, height: 300.0, wordWrap: true);
            var host = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>().CreateTextViewHost(view, setFocus: false);
            var window = new Avalonia.Controls.Window { Width = 400, Height = 300, Content = host.HostControl };
            window.Show();

            try
            {
                var bar = (Avalonia.Controls.Primitives.ScrollBar)host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!.VisualElement;
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(bar.Maximum > 0.0, $"There is a range to scroll through, not one the viewport fills (maximum {bar.Maximum}).");
                Assert.IsTrue(bar.ViewportSize < 8.0, $"The thumb stands for the lines in view, not the whole document (viewport {bar.ViewportSize}).");

                view.DisplayTextLineContainingBufferPosition(
                    new Microsoft.VisualStudio.Text.SnapshotPoint(view.TextSnapshot, view.TextSnapshot.Length), 0.0, ViewRelativePosition.Bottom);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.AreEqual(bar.Maximum, bar.Value, 0.5, "At the end of the document the thumb stands at the end of the track.");
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DraggingTheThumbToTheBottomShowsTheLastLineWhole(bool wrapped)
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // wrapped, the lines at the end take several rows each, so the end holds fewer lines than the start shows
            string text = string.Join('\n', Enumerable.Range(0, 120).Select(i => $"line {i} " + string.Join(' ', Enumerable.Repeat("word", wrapped && i >= 100 ? 60 : 2))));
            var view = HeadlessEditor.CreateView(text, height: 307.0, wordWrap: wrapped);
            var host = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>().CreateTextViewHost(view, setFocus: false);
            var window = new Avalonia.Controls.Window { Width = 400, Height = 307, Content = host.HostControl };
            window.Show();

            try
            {
                var bar = (VerticalScrollBarMarginProvider.VerticalScrollBarMargin)host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar)!;
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                // a viewport seven tenths of a line over a whole number of lines, which a range rounded to whole lines
                // falls short of
                double rows = view.ViewportHeight / view.LineHeight;
                window.Height += (0.7 - rows % 1.0 + 1.0) % 1.0 * view.LineHeight;
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.AreEqual(0.7, view.ViewportHeight / view.LineHeight % 1.0, 0.1, "The viewport is no whole number of lines tall.");
                Assert.IsTrue(bar.Maximum > 0.0, "There is a range to scroll through.");

                // Act: the thumb at the end of the track, where a drag past its bottom leaves it (the test app gives
                // the bar no template, so there is no thumb to drag)
                bar.ScrollTo(bar.Maximum);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                // Assert
                var last = view.TextViewLines.LastVisibleLine;
                Assert.AreEqual(view.TextSnapshot.Length, last.End.Position, "The document's last line is in view.");
                Assert.AreEqual(VisibilityState.FullyVisible, last.VisibilityState, "And whole, not cut off under the bottom edge.");
                Assert.AreEqual(bar.Maximum, bar.Value, 1e-9, "The thumb stays at the end of the track.");
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }
}
