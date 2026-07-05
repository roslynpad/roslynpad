using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// A test view model provider gated on a custom role, so only views explicitly requesting
/// the role get it (the vacuous model serves everything else).
/// </summary>
[Export(typeof(ITextViewModelProvider))]
[ContentType("text")]
[TextViewRole(CustomRole)]
public sealed class TestTextViewModelProvider : ITextViewModelProvider
{
    public const string CustomRole = "TESTVIEWMODEL";

    public ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles) =>
        new TestViewModel(dataModel);

    internal sealed class TestViewModel(ITextDataModel dataModel) : ITextViewModel
    {
        public ITextDataModel DataModel => dataModel;

        public ITextBuffer DataBuffer => dataModel.DataBuffer;

        public ITextBuffer EditBuffer => dataModel.DocumentBuffer;

        public ITextBuffer VisualBuffer => dataModel.DocumentBuffer;

        public PropertyCollection Properties { get; } = new();

        public void Dispose()
        {
        }

        public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint) => editBufferPoint;

        public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode)
            => editBufferPoint.TranslateTo(targetVisualSnapshot, trackingMode);

        public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity) => true;
    }
}

/// <summary>
/// M5: ITextViewModelProvider exports are MEF-discovered (content type and role scoped) at
/// view creation — the seam through which elision/projection view models arrive.
/// </summary>
[TestClass]
public sealed class TextViewModelProviderTests
{
    [TestMethod]
    public async Task MatchingProviderSuppliesTheViewModel()
    {
        await Microsoft.VisualStudio.GeometryTests.HeadlessEditor.RunAsync(() =>
        {
            var bufferFactory = Microsoft.VisualStudio.GeometryTests.HeadlessEditor.Container.GetExport<ITextBufferFactoryService>();
            var contentTypes = Microsoft.VisualStudio.GeometryTests.HeadlessEditor.Container.GetExport<IContentTypeRegistryService>();
            var factory = Microsoft.VisualStudio.GeometryTests.HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var buffer = bufferFactory.CreateTextBuffer("content", contentTypes.GetContentType("text"));

            // A view requesting the provider's role gets its model.
            var roles = factory.CreateTextViewRoleSet(PredefinedTextViewRoles.Interactive, TestTextViewModelProvider.CustomRole);
            var view = factory.CreateTextView(buffer, roles);
            Assert.IsInstanceOfType<TestTextViewModelProvider.TestViewModel>(view.TextViewModel);
            view.Close();

            // Default roles do not match the provider: the vacuous model serves the view.
            var plain = factory.CreateTextView(buffer);
            Assert.IsNotInstanceOfType<TestTextViewModelProvider.TestViewModel>(plain.TextViewModel);
            plain.Close();
        }).ConfigureAwait(false);
    }
}
