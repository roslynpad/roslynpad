using System.Composition;

using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// A test line transform source: every line whose text contains "%pad%" gets extra space
/// above and below and a vertical scale, exercising the M5 aggregation path.
/// </summary>
[Export(typeof(ILineTransformSourceProvider))]
[ContentType("text")]
public sealed class TestLineTransformSourceProvider : ILineTransformSourceProvider
{
    public const double TopSpace = 12.0;
    public const double BottomSpace = 5.0;
    public const double VerticalScale = 2.0;

    public ILineTransformSource Create(ITextView textView) => new Source();

    private sealed class Source : ILineTransformSource
    {
        public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement) =>
            line.Extent.GetText().Contains("%pad%", StringComparison.Ordinal)
                ? new LineTransform(TopSpace, BottomSpace, VerticalScale)
                : new LineTransform(0.0, 0.0, 1.0);
    }
}

/// <summary>
/// M5: line transform sources are MEF-discovered per view and combined with each line's
/// default (adornment-driven) transform at layout, transforming the line's rendered
/// geometry per the LineTransform contract.
/// </summary>
[TestClass]
public sealed class LineTransformTests
{
    [TestMethod]
    public async Task LineTransformSourceChangesLineGeometry()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("plain one\n%pad% line\nplain two");
            var lines = view.TextViewLines;
            var plain = lines[0];
            var padded = lines[1];
            var following = lines[2];

            // The view aggregates the discovered sources.
            Assert.IsNotNull(view.LineTransformSource);

            // Rendered height per contract: (text height + top + bottom) with the scale
            // applied to the text only (LineTransform(top, bottom, scale) overload).
            Assert.AreEqual(1.0, plain.LineTransform.VerticalScale, 0.001);
            Assert.AreEqual(TestLineTransformSourceProvider.TopSpace, padded.LineTransform.TopSpace, 0.001);
            Assert.AreEqual(TestLineTransformSourceProvider.BottomSpace, padded.LineTransform.BottomSpace, 0.001);
            Assert.AreEqual(TestLineTransformSourceProvider.VerticalScale, padded.LineTransform.VerticalScale, 0.001);

            double expectedHeight =
                TestLineTransformSourceProvider.TopSpace
                + (padded.TextHeight * TestLineTransformSourceProvider.VerticalScale)
                + TestLineTransformSourceProvider.BottomSpace;
            Assert.AreEqual(expectedHeight, padded.Height, 0.01);
            Assert.AreEqual(padded.Top + TestLineTransformSourceProvider.TopSpace, padded.TextTop, 0.01);

            // The layout stacks the transformed heights densely.
            Assert.AreEqual(plain.Bottom, padded.Top, 0.01);
            Assert.AreEqual(padded.Bottom, following.Top, 0.01);
        }).ConfigureAwait(false);
    }
}
