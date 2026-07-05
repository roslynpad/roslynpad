//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Formatting.
//  IWpfTextViewLineCollection"). Signature-adapted per PLAN §4.2: WPF Geometry
//  becomes Avalonia.Media.Geometry.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using System.Collections.ObjectModel;
    using Avalonia.Media;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A read-only collection of the <see cref="IWpfTextViewLine"/> objects rendered in a view.
    /// </summary>
    public interface IWpfTextViewLineCollection : ITextViewLineCollection
    {
        /// <summary>
        /// Gets the collection of <see cref="IWpfTextViewLine"/> objects.
        /// </summary>
        ReadOnlyCollection<IWpfTextViewLine> WpfTextViewLines { get; }

        /// <summary>
        /// Gets the <see cref="IWpfTextViewLine"/> containing the given buffer position.
        /// </summary>
        new IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the first line that is visible or partially visible.
        /// </summary>
        new IWpfTextViewLine FirstVisibleLine { get; }

        /// <summary>
        /// Gets the last line that is visible or partially visible.
        /// </summary>
        new IWpfTextViewLine LastVisibleLine { get; }

        /// <summary>
        /// Gets the <see cref="IWpfTextViewLine"/> at the given index.
        /// </summary>
        new IWpfTextViewLine this[int index] { get; }

        /// <summary>
        /// Creates a geometry that outlines the text of the given span. The bounds of each
        /// spanned line segment are the bounds of the rendered text.
        /// </summary>
        Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan);

        /// <summary>
        /// Creates a geometry that outlines the text of the given span, optionally clipped to the
        /// viewport and padded.
        /// </summary>
        Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Avalonia.Thickness padding);

        /// <summary>
        /// Creates a geometry that outlines the given span line-by-line: the bounds of each spanned
        /// line extend to the full width of the line (including the end-of-line whitespace).
        /// </summary>
        Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan);

        /// <summary>
        /// Creates a line-marker geometry for the given span, optionally clipped to the viewport and padded.
        /// </summary>
        Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Avalonia.Thickness padding);

        /// <summary>
        /// Creates a marker geometry for the given span: a text-marker geometry if the span fits in
        /// a single line, otherwise a line-marker geometry.
        /// </summary>
        Geometry GetMarkerGeometry(SnapshotSpan bufferSpan);

        /// <summary>
        /// Creates a marker geometry for the given span, optionally clipped to the viewport and padded.
        /// </summary>
        Geometry GetMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Avalonia.Thickness padding);
    }
}
