using Avalonia.Media;

using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Outlining;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// The pill a collapsed region shows in its place, and the hint that previews the region while the pointer rests on
/// the pill.
/// </summary>
[TestClass]
public sealed class CollapsedRegionAdornmentTests
{
    [TestMethod]
    public async Task TheCollapsedHintStandsOnTheViewsOwnBackground()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("header <<\nhidden one\nhidden two >>\nfooter");
            var manager = HeadlessEditor.Container.GetExport<IOutliningManagerService>().GetOutliningManager(view)!;
            var snapshot = view.TextSnapshot;
            manager.TryCollapse(manager.GetAllRegions(new SnapshotSpan(snapshot, 0, snapshot.Length)).Single());
            Assert.IsTrue(
                view.Properties.TryGetProperty(typeof(CollapsedRegionAdornmentProvider.CollapsedRegionTagger), out CollapsedRegionAdornmentProvider.CollapsedRegionTagger tagger),
                "A collapsed region has its pill, and the tagger that shows its hint.");

            // A host dresses the view for its theme; the hint previews the view's lines, so it stands on that ground
            // rather than on the format map's, which another editor of the process may have set.
            var ground = Color.FromRgb(0x1F, 0x2A, 0x3A);
            view.Background = new SolidColorBrush(ground);
            Assert.AreEqual(ground, ((ISolidColorBrush)tagger.GetEditorBackground(Brushes.Magenta)).Color);

            view.Close();
        }).ConfigureAwait(false);
    }
}
