using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// M4 acceptance: margins are MEF-discovered, ordered within their containers, and
/// removable (option-driven); the vertical scrollbar implements IVerticalScrollBar mapping.
/// </summary>
[TestClass]
public sealed class MarginTests
{
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
}
