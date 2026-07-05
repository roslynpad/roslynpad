using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// M5 acceptance: a WPF editor extension recompiles against the contract assemblies with
/// mechanical-only diffs and runs. The extension lives in Microsoft.VisualStudio.ExtensionConformance,
/// which references Def projects only (no Morgania implementation) — its compiling at all
/// is half the assertion; this test composes it into the real editor and drives it.
/// </summary>
[TestClass]
public sealed class ExtensionConformanceTests
{
    [TestMethod]
    public async Task RecompiledWpfExtensionComposesAndRuns()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            using var container = HeadlessEditor.CreateContainer(typeof(TodoExtension.TodoTaggerProvider).Assembly);
            var bufferFactory = container.GetExport<ITextBufferFactoryService>();
            var contentTypes = container.GetExport<IContentTypeRegistryService>();
            var editorFactory = container.GetExport<ITextEditorFactoryService>();

            var buffer = bufferFactory.CreateTextBuffer(
                "// TODO first\nplain line\n// TODO second",
                contentTypes.GetContentType("text"));
            var view = editorFactory.CreateTextView(buffer);
            var host = editorFactory.CreateTextViewHost(view, setFocus: false);
            view.DisplayTextLineContainingBufferPosition(
                new SnapshotPoint(buffer.CurrentSnapshot, 0), 0.0, ViewRelativePosition.Top, 800.0, 300.0);

            // The extension's creation listener ran and populated its own exported layer.
            var layer = view.GetAdornmentLayer(TodoExtension.TodoAdornmentTextViewCreationListener.LayerName);
            Assert.IsFalse(layer.IsEmpty, "The extension highlighted TODOs on its adornment layer.");

            // The extension's margin was discovered into the host's Bottom container.
            Assert.IsNotNull(host.GetTextViewMargin("TodoMargin"));

            // The extension's tagger flows through the view tag aggregator.
            var aggregator = container
                .GetExport<IViewTagAggregatorFactoryService>()
                .CreateTagAggregator<TextMarkerTag>(view);
            int tagCount = aggregator
                .GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length))
                .Count();
            Assert.AreEqual(2, tagCount, "Both TODOs are tagged.");

            host.Close();
        }).ConfigureAwait(false);
    }
}
