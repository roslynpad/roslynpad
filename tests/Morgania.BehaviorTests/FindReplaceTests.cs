using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// The find/replace panel: created for every interactive view, hidden until shown, floating
/// over the view (outside the zoom transform), navigating and replacing through the editor
/// core's search services, and highlighting viewport matches on its adornment layer.
/// </summary>
[TestClass]
public sealed class FindReplaceTests
{
    [TestMethod]
    public async Task PanelIsCreatedPerViewAndHiddenUntilShown()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("alpha beta alpha");
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);

            var panel = FindReplacePanel.Get(view);
            Assert.IsNotNull(panel);
            Assert.IsFalse(panel.IsOpen);

            panel.Show();
            Assert.IsTrue(panel.IsOpen);

            panel.Hide();
            Assert.IsFalse(panel.IsOpen);

            host.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FindNextSelectsSuccessiveMatchesAndWraps()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("cat dog cat dog cat");
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var panel = FindReplacePanel.Get(view)!;

            panel.Show();
            panel.SearchText = "cat";

            panel.FindNext();
            Assert.AreEqual(new Span(0, 3), view.Selection.StreamSelectionSpan.SnapshotSpan.Span);
            panel.FindNext();
            Assert.AreEqual(new Span(8, 3), view.Selection.StreamSelectionSpan.SnapshotSpan.Span);
            panel.FindNext();
            Assert.AreEqual(new Span(16, 3), view.Selection.StreamSelectionSpan.SnapshotSpan.Span);

            // Wraps past the end back to the first match.
            panel.FindNext();
            Assert.AreEqual(new Span(0, 3), view.Selection.StreamSelectionSpan.SnapshotSpan.Span);

            // And backwards, wrapping to the last match.
            panel.FindPrevious();
            Assert.AreEqual(new Span(16, 3), view.Selection.StreamSelectionSpan.SnapshotSpan.Span);

            host.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ShowSeedsTheSearchFromTheSelectionAndHighlightsMatches()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("cat dog cat dog cat");
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var panel = FindReplacePanel.Get(view)!;

            view.Selection.Select(new SnapshotSpan(view.TextSnapshot, 0, 3), false);
            panel.Show();
            Assert.AreEqual("cat", panel.SearchText);

            // All three matches in the viewport are highlighted on the panel's layer.
            var layer = view.GetAdornmentLayer(FindReplacePanel.HighlightLayerName);
            Assert.AreEqual(3, layer.Elements.Count);

            // Closing the panel clears the highlights.
            panel.Hide();
            Assert.AreEqual(0, layer.Elements.Count);

            host.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplaceNextSelectsFirstThenReplaces()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("cat dog cat");
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var panel = FindReplacePanel.Get(view)!;

            panel.Show(showReplace: true);
            panel.SearchText = "cat";
            panel.ReplaceText = "bird";

            // The first invocation only selects the match it is about to replace.
            panel.ReplaceNext();
            Assert.AreEqual("cat dog cat", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(new Span(0, 3), view.Selection.StreamSelectionSpan.SnapshotSpan.Span);

            // The second replaces it and moves to the next match.
            panel.ReplaceNext();
            Assert.AreEqual("bird dog cat", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(new Span(9, 3), view.Selection.StreamSelectionSpan.SnapshotSpan.Span);

            panel.ReplaceNext();
            Assert.AreEqual("bird dog bird", view.TextBuffer.CurrentSnapshot.GetText());

            host.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplaceAllReplacesEveryMatchInASingleEdit()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("cat dog cat dog cat");
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var panel = FindReplacePanel.Get(view)!;

            panel.Show(showReplace: true);
            panel.SearchText = "cat";
            panel.ReplaceText = "bird";

            int versionBefore = view.TextBuffer.CurrentSnapshot.Version.VersionNumber;
            panel.ReplaceAll();
            Assert.AreEqual("bird dog bird dog bird", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(versionBefore + 1, view.TextBuffer.CurrentSnapshot.Version.VersionNumber);

            host.Close();
        }).ConfigureAwait(false);
    }
}
