//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Formatting.
//  IFormattedLineSource"). Signature-adapted per PLAN §4.2: WPF
//  TextRunProperties becomes Avalonia TextRunProperties.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using System.Collections.ObjectModel;
    using Avalonia.Media.TextFormatting;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Formats the text of snapshot lines into <see cref="IFormattedLine"/> objects (one per
    /// visual row; more than one when word wrap is enabled). All measurements are in the text
    /// rendering coordinate system.
    /// </summary>
    public interface IFormattedLineSource
    {
        /// <summary>
        /// Gets the text snapshot of the view's edit buffer on which formatting is based.
        /// </summary>
        ITextSnapshot SourceTextSnapshot { get; }

        /// <summary>
        /// Gets the text snapshot of the view's visual buffer on which formatting is based.
        /// </summary>
        ITextSnapshot TopTextSnapshot { get; }

        /// <summary>
        /// Determines whether the text is formatted with display-mode (ideal metrics disabled) settings.
        /// </summary>
        bool UseDisplayMode { get; }

        /// <summary>
        /// Gets the number of spaces between tab stops.
        /// </summary>
        int TabSize { get; }

        /// <summary>
        /// Gets the x-coordinate of the leading edge of unindented text.
        /// </summary>
        double BaseIndentation { get; }

        /// <summary>
        /// Gets the x-coordinate at which text wraps, or 0.0 when word wrap is disabled.
        /// </summary>
        double WordWrapWidth { get; }

        /// <summary>
        /// Gets the maximum indentation applied to the continuation of word-wrapped lines.
        /// </summary>
        double MaxAutoIndent { get; }

        /// <summary>
        /// Gets the width of a column (the width of a space in the default text properties).
        /// </summary>
        double ColumnWidth { get; }

        /// <summary>
        /// Gets the nominal height of a line of text formatted with the default text properties.
        /// </summary>
        double LineHeight { get; }

        /// <summary>
        /// Gets the height of the text above the baseline in a nominal line.
        /// </summary>
        double TextHeightAboveBaseline { get; }

        /// <summary>
        /// Gets the height of the text below the baseline in a nominal line.
        /// </summary>
        double TextHeightBelowBaseline { get; }

        /// <summary>
        /// Gets the default text properties used to format text in this view.
        /// </summary>
        TextRunProperties DefaultTextProperties { get; }

        /// <summary>
        /// Formats the given snapshot line of the visual buffer, producing one
        /// <see cref="IFormattedLine"/> per visual row.
        /// </summary>
        Collection<IFormattedLine> FormatLineInVisualBuffer(ITextSnapshotLine visualLine);
    }
}
