namespace Microsoft.VisualStudio.Demo;

using System.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// M5 line transforms: the "// ---- block N ----" separator lines get extra breathing
/// room above and below, the way CodeLens and inline-diff bands reserve vertical space in
/// VS. The editor aggregates this source with its own via <see cref="ILineTransformSource"/>.
/// </summary>
[Export(typeof(ILineTransformSourceProvider))]
[ContentType("code")]
public sealed class BlockSeparatorLineTransformProvider : ILineTransformSourceProvider
{
    public ILineTransformSource Create(ITextView textView) => BlockSeparatorLineTransformSource.Instance;

    private sealed class BlockSeparatorLineTransformSource : ILineTransformSource
    {
        public static readonly BlockSeparatorLineTransformSource Instance = new();

        public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
        {
            // Pad only the first row of a (possibly word-wrapped) separator line.
            var snapshotLine = line.Start.GetContainingLine();
            return line.Start == snapshotLine.Start
                && snapshotLine.GetText().StartsWith("// ---- block", StringComparison.Ordinal)
                ? new LineTransform(14.0, 6.0, 1.0)
                : new LineTransform(0.0, 0.0, 1.0);
        }
    }
}
